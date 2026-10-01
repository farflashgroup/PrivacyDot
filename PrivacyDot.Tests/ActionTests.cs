using PrivacyDot;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

internal static class ActionTests
{
    private static readonly DeviceUsageEntry Microphone = new(DeviceKind.Microphone, "Google Chrome",
        @"C:\Program Files\Google\Chrome\Application\chrome.exe", UsageSource.CoreAudio);
    private static readonly DeviceUsageEntry Camera = new(DeviceKind.Camera, "Google Chrome", Microphone.Identity, UsageSource.PrivacyRegistry);
    private static DeviceUsageSnapshot Snapshot => DeviceUsageSnapshot.FromEntries(new[] { Microphone, Camera });

    public static void TestIdentityMatching()
    {
        Assert(ProcessActions.MatchesIdentity(Microphone.Identity.ToUpperInvariant(), Microphone.Identity, null), "Path comparison must ignore case.");
        Assert(!ProcessActions.MatchesIdentity("chrome", Microphone.Identity, null), "Names must not match paths.");
        Assert(!ProcessActions.MatchesIdentity(@"C:\Other\chrome.exe", Microphone.Identity, null), "Same filename in another directory must not match.");
        Assert(!ProcessActions.MatchesIdentity("Example", null, "Example_123"), "Partial package names must not match.");
        Assert(ProcessActions.MatchesIdentity("Example_123", null, "Example_123"), "Full package identity must match.");
        Assert(new ProcessActions().PrepareQuit(new DeviceUsageEntry(DeviceKind.Camera, "Unknown", "pid:123:chrome", UsageSource.CoreAudio)) is null,
            "An unverified PID must never be used for force quit.");
    }

    public static void TestConfirmation()
    {
        var actions = new FakeActions();
        double now = 0;
        using var confirmation = new QuitConfirmation(actions, () => now);
        Assert(confirmation.Click(Microphone) == QuitResult.Armed && actions.Executed == 0, "First click must only arm.");
        Assert(confirmation.Click(Microphone) == QuitResult.TooSoon && actions.Executed == 0, "Double click must not quit.");
        now = 3;
        Assert(confirmation.Click(Microphone) == QuitResult.Completed && actions.Executed == 1, "Second deliberate click must quit.");
        Assert(actions.Disposed == 1 && confirmation.ProcessCount == 0, "Completed request must release its handles.");
        confirmation.Click(Microphone);
        now = 14;
        Assert(confirmation.Expire() && confirmation.ProcessCount == 0, "Confirmation must expire.");
        Assert(confirmation.Click(Microphone) == QuitResult.Armed && actions.Executed == 1, "Expired click must re-arm.");
        now = 17;
        Assert(confirmation.Click(Camera) == QuitResult.Armed && actions.Executed == 1, "Changing device row must re-arm.");
        confirmation.Cancel();
        Assert(actions.Disposed == 4, "Switching and cancelling must release every request.");
    }

    public static void TestFailures()
    {
        var actions = new FakeActions { Available = false };
        double now = 0;
        using var confirmation = new QuitConfirmation(actions, () => now);
        Assert(confirmation.Click(Microphone) == QuitResult.Unavailable, "Unavailable processes must not arm.");
        actions.Available = true;
        actions.Success = false;
        confirmation.Click(Microphone);
        now = 3;
        Assert(confirmation.Click(Microphone) == QuitResult.Failed, "A native failure must not report success.");
        Assert(confirmation.ProcessCount == 0 && actions.Disposed == 1, "Failure must clear the request.");
    }

    public static void TestNativeQuit()
    {
        // Only launch and terminate disposable test-owned processes in unique paths.
        var directory = Path.Combine(Path.GetTempPath(), "PrivacyDot-action-test-" + Guid.NewGuid().ToString("N"));
        var otherDirectory = Path.Combine(directory, "other");
        Directory.CreateDirectory(otherDirectory);
        var executable = typeof(ActionTests).Assembly.Location;
        var app = typeof(DeviceUsageEntry).Assembly.Location;
        foreach (var folder in new[] { directory, otherDirectory })
        {
            File.Copy(executable, Path.Combine(folder, "Helper.exe"));
            File.Copy(app, Path.Combine(folder, "PrivacyDot.exe"));
        }
        var helpers = new List<Process>();
        try
        {
            var first = StartHelper(directory, helpers);
            var second = StartHelper(directory, helpers);
            var unrelated = StartHelper(otherDirectory, helpers);
            var entry = new DeviceUsageEntry(DeviceKind.Microphone, "Test helper", Path.Combine(directory, "Helper.exe"), UsageSource.CoreAudio);
            using var request = new ProcessActions().PrepareQuit(entry);
            Assert(request is not null && request.Count == 2, "Must capture only both exact-path helpers.");
            Assert(!first.HasExited && !second.HasExited, "Preparing must not terminate anything.");
            var late = StartHelper(directory, helpers);
            Assert(request!.Execute(), "Native termination should succeed for test-owned helpers.");
            Assert(first.WaitForExit(3000) && second.WaitForExit(3000), "Captured helpers should exit.");
            Assert(!late.HasExited && !unrelated.HasExited, "New processes and look-alike paths must remain running.");
            Assert(request.Execute(), "Already-exited targets must be harmless.");
        }
        finally
        {
            foreach (var helper in helpers)
            {
                if (!helper.HasExited) { helper.Kill(); helper.WaitForExit(3000); }
                helper.Dispose();
            }
            var fullPath = Path.GetFullPath(directory);
            var tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (fullPath.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)
                && Path.GetFileName(fullPath).StartsWith("PrivacyDot-action-test-", StringComparison.Ordinal))
                Directory.Delete(fullPath, recursive: true);
        }
    }

    private static Process StartHelper(string directory, List<Process> helpers)
    {
        var process = Process.Start(new ProcessStartInfo(Path.Combine(directory, "Helper.exe"), "--process-action-helper")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        })!;
        helpers.Add(process);
        var ready = process.StandardOutput.ReadLineAsync();
        Assert(ready.Wait(5000) && ready.Result == "READY", "Helper must start successfully.");
        return process;
    }

    public static void TestPopup()
    {
        var previousLanguage = Localization.SelectedCultureName;
        try
        {
            foreach (var language in new[] { "en", "fr", "ar" })
            {
                Localization.SetLanguage(language, persist: false);
                var actions = new FakeActions();
                double now = 0;
                using var popup = new UsagePopupForm(Snapshot, ThemePalette.Dark, false, actions, () => now);
                popup.Location = new Point(-10000, -10000);
                popup.Show();
                Application.DoEvents();
                var buttons = Buttons(popup).ToArray();
                Assert(buttons.Length == 4, "Each row needs two actions.");
                Assert(buttons.All(button => !string.IsNullOrWhiteSpace(button.AccessibleName)), "All icon buttons need accessible names.");
                Assert(buttons.Count(button => button.Unavailable) == 2, "Device actions must be explicitly unavailable.");
                Assert(buttons.All(button => button.PointToScreen(Point.Empty).X > popup.Left + popup.Width / 2), "Actions must stay to the right, including RTL.");
                buttons.First(button => button.Glyph == UsageActionGlyph.MicrophoneOff).PerformClick();
                Assert(Labels(popup).Any(label => label.Text == Localization.Get(AppText.DeviceControlExplanation)), "Unavailable action must explain itself.");
                Buttons(popup).First(button => button.Glyph == UsageActionGlyph.Quit).PerformClick();
                Assert(actions.Executed == 0 && Buttons(popup).Count() == 3, "Warning must replace both row icons without executing.");
                Buttons(popup).Single(button => button.Glyph == UsageActionGlyph.Warning).PerformClick();
                Assert(actions.Executed == 0, "Rapid second click must not terminate.");
                now = 3;
                Buttons(popup).Single(button => button.Glyph == UsageActionGlyph.Warning).PerformClick();
                Assert(actions.Executed == 1 && Buttons(popup).Count() == 4, "Second deliberate click must execute and restore icons.");
                Buttons(popup).First(button => button.Glyph == UsageActionGlyph.Quit).PerformClick();
                popup.UpdateSnapshot(Snapshot);
                Assert(!Buttons(popup).Any(button => button.Glyph == UsageActionGlyph.Warning), "Refresh must cancel pending quit.");
                Assert(Labels(popup).Any(label => label.AutoEllipsis), "Long identities must use ellipsis.");
                Assert(Labels(popup).Where(label => label.Text == Microphone.Identity)
                    .All(label => label.RightToLeft == RightToLeft.No && label.Width > 100 && label.Height > 5),
                    "Executable paths must remain readable in every language.");
                Buttons(popup).First(button => button.Glyph == UsageActionGlyph.Quit).PerformClick();
                var escape = typeof(UsagePopupForm).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                escape.Invoke(popup, new object[] { new Message(), Keys.Escape });
                Assert(!popup.IsDisposed && !Buttons(popup).Any(button => button.Glyph == UsageActionGlyph.Warning), "Escape must cancel before closing.");
                Buttons(popup).First(button => button.Glyph == UsageActionGlyph.Quit).PerformClick();
                var deactivate = typeof(UsagePopupForm).GetMethod("OnDeactivate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                deactivate.Invoke(popup, new object[] { EventArgs.Empty });
                Assert(!Buttons(popup).Any(button => button.Glyph == UsageActionGlyph.Warning), "Deactivation must cancel confirmation.");
                popup.Close();
            }
        }
        finally { Localization.SetLanguage(previousLanguage, persist: false); }
    }

    public static void TestPopupShutdown()
    {
        for (var iteration = 0; iteration < 5; iteration++)
        {
            using var popup = new UsagePopupForm(Snapshot, ThemePalette.Dark, false, new FakeActions());
            popup.Location = new Point(-10000, -10000);
            popup.Show();
            Application.DoEvents();
            Buttons(popup).First(button => button.Glyph == UsageActionGlyph.Quit).PerformClick();
            // Direct disposal delivers deactivation while child controls still
            // reference the popup's fonts. It must neither rebuild nor throw.
            popup.Dispose();
            Application.DoEvents();
            Assert(popup.IsDisposed, "Popup should dispose cleanly with a warning armed.");
        }
    }

    public static void RenderScreenshots(string directory)
    {
        Directory.CreateDirectory(directory);
        Application.EnableVisualStyles();
        Localization.SetLanguage("en", persist: false);
        foreach (var theme in new[] { ThemePalette.Dark, ThemePalette.Light })
        {
            var name = theme.IsDark ? "dark" : "light";
            using var popup = new UsagePopupForm(Snapshot, theme, false, new FakeActions());
            popup.Location = new Point(-10000, -10000);
            popup.Show();
            SavePopup(popup, Path.Combine(directory, name + ".png"));
            Buttons(popup).First(button => button.Glyph == UsageActionGlyph.Quit).PerformClick();
            SavePopup(popup, Path.Combine(directory, name + "-warning.png"));
            popup.UpdateSnapshot(Snapshot);
            Buttons(popup).First(button => button.Glyph == UsageActionGlyph.CameraOff).PerformClick();
            SavePopup(popup, Path.Combine(directory, name + "-unavailable.png"));
        }
        Localization.SetLanguage("ar", persist: false);
        using var arabic = new UsagePopupForm(Snapshot, ThemePalette.Dark, false, new FakeActions());
        arabic.Location = new Point(-10000, -10000);
        arabic.Show();
        Buttons(arabic).First(button => button.Glyph == UsageActionGlyph.Quit).PerformClick();
        SavePopup(arabic, Path.Combine(directory, "arabic-warning.png"));
        Localization.SetLanguage("en", persist: false);
        var manyEntries = Enumerable.Range(0, 20).Select(index => new DeviceUsageEntry(DeviceKind.Microphone,
            "Very long application name with many words " + index, @"C:\Very long application directory\" + index + @"\application.exe", UsageSource.CoreAudio));
        using var overflow = new UsagePopupForm(DeviceUsageSnapshot.FromEntries(manyEntries), ThemePalette.Dark, false, new FakeActions());
        overflow.Location = new Point(-10000, -10000);
        overflow.Show();
        SavePopup(overflow, Path.Combine(directory, "overflow.png"));
    }

    private static void SavePopup(Form popup, string path)
    {
        Application.DoEvents();
        popup.PerformLayout();
        popup.Refresh();
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
    private static IEnumerable<UsageActionButton> Buttons(Control control) => Descendants<UsageActionButton>(control);
    private static IEnumerable<Label> Labels(Control control) => Descendants<Label>(control);
    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class FakeActions : IProcessActions
    {
        public int Executed;
        public int Disposed;
        public bool Available = true;
        public bool Success = true;
        public IPreparedProcessQuit? PrepareQuit(DeviceUsageEntry entry) => Available ? new FakeRequest(this) : null;
        private sealed class FakeRequest : IPreparedProcessQuit
        {
            private readonly FakeActions _owner;
            public FakeRequest(FakeActions owner) => _owner = owner;
            public int Count => 2;
            public bool Execute() { _owner.Executed++; return _owner.Success; }
            public void Dispose() => _owner.Disposed++;
        }
    }
}
