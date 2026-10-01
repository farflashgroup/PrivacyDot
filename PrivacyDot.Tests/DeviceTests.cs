using PrivacyDot;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

internal static class DeviceTests
{
    private static DeviceUsageEntry App(string app, string device, string name, DeviceKind kind = DeviceKind.Microphone)
        => new(kind, app, $@"C:\Apps\{app}.exe", kind == DeviceKind.Microphone ? UsageSource.CoreAudio : UsageSource.CameraActivity, device, name);

    public static DeviceUsageSnapshot Sample => DeviceUsageSnapshot.FromEntries(new[]
    {
        App("Google Chrome", "mic-usb", "USB Microphone"),
        App("Zoom", "mic-headset", "Headset Microphone"),
        App("Google Chrome", "camera-usb", "Logitech Brio", DeviceKind.Camera),
        App("Zoom", "camera-built-in", "Integrated Camera", DeviceKind.Camera)
    });

    public static void TestMultipleDevices()
    {
        var usb = App("Chrome", "mic-usb", "USB Microphone");
        var headset = usb.WithDevice("mic-headset", "Headset");
        var snapshot = DeviceUsageSnapshot.FromEntries(new[] { usb, headset, usb,
            usb.WithDevice(null, null).WithSource(UsageSource.PrivacyRegistry) });
        Assert(snapshot.MicrophoneApps.Count == 2, "One app on two endpoints must retain two rows, without a registry duplicate.");
        Assert(DeviceUsageGroup.FromEntries(snapshot.MicrophoneApps).Count == 2, "Devices must have separate groups.");
        var twoApps = DeviceUsageSnapshot.FromEntries(new[] { usb, App("Zoom", "mic-usb", "USB Microphone") });
        Assert(DeviceUsageGroup.FromEntries(twoApps.MicrophoneApps).Single().Apps.Count == 2, "Two programs on one device must share a group.");
        var cameras = DeviceUsageSnapshot.FromEntries(new[] { App("Chrome", "camera-a", "Camera", DeviceKind.Camera),
            App("Chrome", "camera-b", "Camera", DeviceKind.Camera) });
        Assert(cameras.CameraApps.Count == 2, "One app on two cameras must retain two rows.");
        Assert(usb.WithSource(UsageSource.PrivacyRegistry).DeviceId == usb.DeviceId, "Source changes must retain the endpoint.");
    }

    public static void TestFallbackAndNames()
    {
        Localization.SetLanguage("en", persist: false);
        var confirmed = App("Chrome", "mic-a", "USB Microphone");
        var unknown = new DeviceUsageEntry(DeviceKind.Microphone, "Unknown app", @"C:\Apps\unknown.exe", UsageSource.PrivacyRegistry);
        var snapshot = DeviceUsageSnapshot.FromEntries(new[] { confirmed, unknown });
        var groups = DeviceUsageGroup.FromEntries(snapshot.MicrophoneApps);
        Assert(groups.Count == 2 && groups.Last().DeviceId is null, "Unmatched registry records need an unknown-device group.");
        Assert(groups.Last().Name == Localization.Get(AppText.DeviceNotIdentified), "Unknown device must be explicit.");
        var unnamed = DeviceUsageGroup.FromEntries(new[] { confirmed.WithDevice("mic-b", null) }).Single();
        Assert(unnamed.DeviceId == "mic-b" && unnamed.Name == "Microphone", "A missing friendly name must retain device identity.");
        var sameNames = DeviceUsageGroup.FromEntries(new[] { confirmed, confirmed.WithDevice("mic-b", "USB Microphone") });
        Assert(sameNames.Select(group => group.Name).Distinct().Count() == 2, "Identical names must remain visibly distinct.");
        var reverse = DeviceUsageGroup.FromEntries(new[] { confirmed.WithDevice("mic-b", "USB Microphone"), confirmed });
        Assert(sameNames.Select(group => group.Name).SequenceEqual(reverse.Select(group => group.Name)), "Duplicate-name labels must not depend on enumeration order.");
        var names = new DeviceNameRegistry();
        var initial = DeviceUsageGroup.FromEntries(new[] { confirmed, confirmed.WithDevice("mic-b", "USB Microphone") }, names);
        var secondLabel = initial.Single(group => group.DeviceId == "mic-b").Name;
        var changed = DeviceUsageGroup.FromEntries(new[] { confirmed.WithDevice("mic-b", "USB Microphone"), confirmed.WithDevice("mic-c", "USB Microphone") }, names);
        Assert(changed.Single(group => group.DeviceId == "mic-b").Name == secondLabel, "Stopping a device must not transfer a numbered label.");
        Assert(DeviceUsageGroup.FromEntries(new[] { confirmed.WithDevice("mic-b", "USB Microphone") }, names).Single().Name == secondLabel,
            "A remaining same-name device must retain its disambiguation across popup refreshes.");
    }

    public static void TestDeviceChanges()
    {
        var entry = App("Chrome", "mic-a", "USB Microphone");
        var original = DeviceUsageSnapshot.FromEntries(new[] { entry });
        Assert(!original.Equals(DeviceUsageSnapshot.FromEntries(new[] { entry.WithDevice("mic-b", "USB Microphone") })),
            "Switching device must refresh the popup even when the app stays the same.");
        Assert(!original.Equals(DeviceUsageSnapshot.FromEntries(new[] { entry.WithDevice("mic-a", "Renamed") })),
            "Renaming a device must refresh the popup.");
        Assert(original.Equals(DeviceUsageSnapshot.FromEntries(new[] { entry, entry })), "Duplicate reports must not cause a refresh loop.");
    }

    public static void TestCameraActivityUpdates()
    {
        using var cache = new CameraActivityCache();
        var a = new CameraProcessUsage(10, 100, App("Chrome", "camera-a", "Camera A", DeviceKind.Camera));
        var b = new CameraProcessUsage(20, 200, App("Zoom", "camera-b", "Camera B", DeviceKind.Camera));
        var empty = Array.Empty<CameraProcessUsage>();
        cache.ApplyReports(new[] { new CameraDeviceReport("camera-a", new[] { a }), new CameraDeviceReport("camera-b", new[] { b }) }, 1);
        Assert(cache.GetActiveEntries(_ => true).Count == 2, "Both active cameras should be reported.");
        cache.ApplyReports(new[] { new CameraDeviceReport("camera-a", empty) }, 2);
        Assert(cache.GetActiveEntries(_ => true).Single().DeviceId == "camera-b", "Stopping one camera must preserve the other.");
        cache.ApplyReports(new[] { new CameraDeviceReport("camera-a", new[] { a }), new CameraDeviceReport("CAMERA-A", empty) }, 3);
        Assert(cache.GetActiveEntries(_ => true).Count == 2, "Streaming must win conflicting reports within one callback.");
        Assert(cache.GetActiveEntries(process => process.ProcessId != 10).Single().Identity == b.Entry.Identity,
            "Exited/reused processes must not be attributed to a camera.");
        cache.ApplyReports(new[] { new CameraDeviceReport("camera-a", empty), new CameraDeviceReport("camera-b", empty) }, 5);
        Assert(cache.GetActiveEntries(_ => true).Count == 0, "Stop/unplug reports must clear active cameras.");
        cache.ApplyReports(new[] { new CameraDeviceReport("camera-a", new[] { a }) }, 4);
        Assert(cache.GetActiveEntries(_ => true).Count == 0, "A callback that finishes late must not revive a stopped stream.");
        cache.Dispose();
        cache.ApplyReports(new[] { new CameraDeviceReport("camera-a", new[] { a }) }, 6);
        Assert(cache.GetActiveEntries(_ => true).Count == 0, "Late callbacks after shutdown must be ignored.");
    }

    public static void TestRowConfirmation()
    {
        var first = App("Chrome", "mic-a", "USB Microphone");
        var second = first.WithDevice("mic-b", "Headset");
        var actions = new FakeActions();
        double time = 0;
        using var confirmation = new QuitConfirmation(actions, () => time);
        confirmation.Click(first);
        time = 3;
        Assert(!confirmation.IsArmed(second), "Warning belongs to only the selected device row.");
        Assert(confirmation.Click(second) == QuitResult.Armed && actions.Executed == 0,
            "Clicking the same app on another device must not confirm the first row's quit.");
    }

    public static void TestGroupedPopup()
    {
        var previous = Localization.SelectedCultureName;
        try
        {
            foreach (var language in new[] { "en", "fr", "ar" })
            {
                Localization.SetLanguage(language, persist: false);
                using var popup = new UsagePopupForm(Sample, ThemePalette.Dark, false, new FakeActions());
                popup.Location = new Point(-10000, -10000);
                popup.Show();
                Application.DoEvents();
                var labels = Descendants<Label>(popup).Select(label => label.Text).ToArray();
                foreach (var name in new[] { "USB Microphone", "Headset Microphone", "Logitech Brio", "Integrated Camera" })
                    Assert(labels.Contains(name), $"Missing device heading: {name} in {language}.");
                Assert(Descendants<UsageActionButton>(popup).Count() == 8, "Four app/device rows need eight controls.");
                Assert(Descendants<Panel>(popup).Where(panel => panel.AutoScroll).All(panel => !panel.HorizontalScroll.Visible),
                    "Device grouping must not add horizontal scrolling.");
                popup.Close();
            }
        }
        finally { Localization.SetLanguage(previous, persist: false); }
    }

    public static void InspectNativeUsage()
    {
        using var cameras = new CameraActivityReader();
        cameras.Start();
        var elapsed = Stopwatch.StartNew();
        while (elapsed.ElapsedMilliseconds < 2000) { Application.DoEvents(); Thread.Sleep(20); }
        Console.WriteLine($"Camera monitor available: {cameras.IsAvailable}; callbacks received: {cameras.ReportsReceived}");
        foreach (var entry in new CoreAudioUsageReader().GetActiveMicrophoneApps().Concat(cameras.GetActiveCameraApps()))
            Console.WriteLine($"{entry.Kind}: {entry.DeviceName ?? "(unnamed)"} [{entry.DeviceId ?? "unidentified"}] <- {entry.DisplayName}");
    }

    public static void RenderScreenshots(string directory)
    {
        Directory.CreateDirectory(directory);
        Application.EnableVisualStyles();
        foreach (var language in new[] { "en", "ar" })
        {
            Localization.SetLanguage(language, persist: false);
            foreach (var theme in new[] { ThemePalette.Dark, ThemePalette.Light })
            {
                using var popup = new UsagePopupForm(Sample, theme, false, new FakeActions());
                popup.Location = new Point(-10000, -10000);
                popup.Show();
                Application.DoEvents();
                Save(popup, Path.Combine(directory, $"devices-{language}-{(theme.IsDark ? "dark" : "light")}.png"));
            }
        }
        Localization.SetLanguage("en", persist: false);
        var edgeCases = DeviceUsageSnapshot.FromEntries(new[]
        {
            App("Zoom", "mic-a", "USB Microphone"), App("Google Chrome", "mic-b", "USB Microphone"),
            App("Zoom", "camera-a", "A camera with a very long device name that should wrap without losing its identity", DeviceKind.Camera),
            new DeviceUsageEntry(DeviceKind.Camera, "Other app", "Example.OtherApp_123", UsageSource.PrivacyRegistry)
        });
        using var fallback = new UsagePopupForm(edgeCases, ThemePalette.Dark, false, new FakeActions());
        fallback.Location = new Point(-10000, -10000);
        fallback.Show();
        Save(fallback, Path.Combine(directory, "devices-edge-cases.png"));

        var sharedHeadset = DeviceUsageSnapshot.FromEntries(new[]
        {
            App("Google Chrome", "mic-headset", "Headset Microphone"),
            App("Zoom", "mic-headset", "Headset Microphone")
        });
        using var shared = new UsagePopupForm(sharedHeadset, ThemePalette.Dark, false, new FakeActions());
        shared.Location = new Point(-10000, -10000);
        shared.Show();
        Save(shared, Path.Combine(directory, "shared-headset-dark.png"));
    }

    private static void Save(Form popup, string path)
    {
        Application.DoEvents();
        popup.PerformLayout();
        using var bitmap = new Bitmap(popup.ClientSize.Width, popup.ClientSize.Height);
        popup.DrawToBitmap(bitmap, new Rectangle(Point.Empty, popup.ClientSize));
        bitmap.Save(path, ImageFormat.Png);
    }

    private static IEnumerable<T> Descendants<T>(Control control) where T : Control
    {
        foreach (Control child in control.Controls)
        {
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class FakeActions : IProcessActions
    {
        public int Executed;
        public IPreparedProcessQuit PrepareQuit(DeviceUsageEntry entry) => new Request(this);
        private sealed class Request : IPreparedProcessQuit
        {
            private readonly FakeActions _actions;
            public Request(FakeActions actions) => _actions = actions;
            public int Count => 1;
            public bool Execute() { _actions.Executed++; return true; }
            public void Dispose() { }
        }
    }
}
