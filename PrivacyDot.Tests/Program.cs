using PrivacyDot;
using System.Globalization;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

if (args.Length == 2 && string.Equals(args[0], "--render-doc-screenshots", StringComparison.OrdinalIgnoreCase))
{
    Localization.SetLanguage("en", persist: false);
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    RenderDocumentationScreenshots(args[1]);
    return;
}

if (args.Length == 2 && string.Equals(args[0], "--menu-preview", StringComparison.OrdinalIgnoreCase))
{
    Localization.SetLanguage(args[1], persist: false);
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);

    using var context = new TrayApplicationContext();
    using var host = new Form
    {
        ClientSize = new System.Drawing.Size(900, 500),
        RightToLeft = Localization.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No,
        RightToLeftLayout = Localization.IsRightToLeft,
        StartPosition = FormStartPosition.CenterScreen,
        Text = $"PrivacyDot Settings Preview ({args[1]})"
    };
    var menuButton = new Button
    {
        ContextMenuStrip = context.MenuForTesting,
        Dock = DockStyle.Fill,
        Text = "Open PrivacyDot menu"
    };
    host.Controls.Add(menuButton);
    host.Shown += (_, _) => context.MenuForTesting.Show(
        menuButton,
        new System.Drawing.Point(Localization.IsRightToLeft ? 600 : 100, 100));
    Application.Run(host);
    return;
}

if (args.Length == 2 && string.Equals(args[0], "--preview", StringComparison.OrdinalIgnoreCase))
{
    Localization.SetLanguage(args[1], persist: false);
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);

    using var preview = new UsagePopupForm(DeviceUsageSnapshot.Empty, closeOnDeactivate: false)
    {
        FormBorderStyle = FormBorderStyle.FixedSingle,
        ShowInTaskbar = true,
        StartPosition = FormStartPosition.CenterScreen,
        Text = $"PrivacyDot ({args[1]})",
        TopMost = false
    };

    Application.Run(preview);
    return;
}

var tests = new (string Name, Action Run)[]
{
    ("Active when stop is zero", () => Assert(RegistryUsageParser.IsActive(10L, 0L), "Expected active usage.")),
    ("Active when stop predates start", () => Assert(RegistryUsageParser.IsActive(20L, 10L), "Expected active usage.")),
    ("Inactive when stop follows start", () => Assert(!RegistryUsageParser.IsActive(10L, 20L), "Expected inactive usage.")),
    ("Inactive with missing start", () => Assert(!RegistryUsageParser.IsActive(null, 0L), "Expected missing start to be inactive.")),
    ("Malformed values are inactive", () => Assert(!RegistryUsageParser.IsActive("not-a-time", "also-not-a-time"), "Expected malformed values to be inactive.")),
    ("Byte registry values parse", TestByteRegistryValueParsing),
    ("Encoded desktop app path becomes an entry", TestEncodedPathEntry),
    ("Packaged app name is cleaned", TestPackageEntry),
    ("NonPackaged container is skipped", TestNonPackagedContainer),
    ("Snapshot deduplicates registry and Core Audio", TestSnapshotDeduplicatesEntries),
    ("All supported languages have complete translations", TestSupportedTranslations),
    ("Regional cultures resolve to a supported language", TestCultureResolution),
    ("Snapshot status follows the selected language", TestLocalizedSnapshotStatus),
    ("Standard Arabic uses right-to-left layout", TestArabicReadingDirection),
    ("Popup renders localized French and Arabic text", TestLocalizedPopup),
    ("Updater recognizes a newer stable release", TestNewerReleaseAvailable),
    ("Updater treats the installed release as current", TestInstalledReleaseIsCurrent),
    ("Updater rejects an untrusted download URL", TestUntrustedUpdateUrl),
    ("Updater caps release metadata", TestOversizedReleaseMetadata),
    ("Updater caps streamed installer data", TestInstallerStreamSizeLimit),
    ("Updater downloads and verifies the installer", TestVerifiedInstallerDownload),
    ("Settings menu contains Language and updater controls", TestSettingsMenuStructure)
};

foreach (var test in tests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}

Console.WriteLine("All PrivacyDot tests passed.");

static void TestByteRegistryValueParsing()
{
    var bytes = BitConverter.GetBytes(12345L);
    Assert(RegistryUsageParser.ConvertToInt64(bytes) == 12345L, "Expected little-endian byte value to parse.");
}

static void TestEncodedPathEntry()
{
    var created = RegistryUsageParser.TryCreateEntry(
        DeviceKind.Microphone,
        @"C:#Program Files#Sample App#Sample.exe",
        10L,
        0L,
        out var entry);

    Assert(created, "Expected active encoded path to create an entry.");
    Assert(entry is not null, "Expected entry.");
    Assert(entry!.Identity == @"C:\Program Files\Sample App\Sample.exe", $"Unexpected identity: {entry.Identity}");
    Assert(entry.DisplayName == "Sample", $"Unexpected display name: {entry.DisplayName}");
    Assert(entry.Kind == DeviceKind.Microphone, "Expected microphone kind.");
}

static void TestPackageEntry()
{
    var created = RegistryUsageParser.TryCreateEntry(
        DeviceKind.Camera,
        "Microsoft.WindowsCamera_8wekyb3d8bbwe",
        10L,
        0L,
        out var entry);

    Assert(created, "Expected active package to create an entry.");
    Assert(entry is not null, "Expected entry.");
    Assert(entry!.DisplayName == "Microsoft Windows Camera", $"Unexpected display name: {entry.DisplayName}");
    Assert(entry.Kind == DeviceKind.Camera, "Expected camera kind.");
}

static void TestNonPackagedContainer()
{
    var created = RegistryUsageParser.TryCreateEntry(
        DeviceKind.Microphone,
        "NonPackaged",
        10L,
        0L,
        out var entry);

    Assert(!created, "Expected NonPackaged container to be skipped.");
    Assert(entry is null, "Expected no entry.");
}

static void TestSnapshotDeduplicatesEntries()
{
    var registryEntry = new DeviceUsageEntry(
        DeviceKind.Microphone,
        "Sample",
        @"C:\Program Files\Sample App\Sample.exe",
        UsageSource.PrivacyRegistry);

    var coreAudioEntry = registryEntry.WithSource(UsageSource.CoreAudio);
    var snapshot = DeviceUsageSnapshot.FromEntries(new[] { registryEntry, coreAudioEntry });

    Assert(snapshot.MicrophoneApps.Count == 1, "Expected one microphone app after dedupe.");
    Assert(snapshot.MicrophoneApps[0].Source == UsageSource.CoreAudio, "Expected Core Audio entry to win duplicates.");
}

static void TestSupportedTranslations()
{
    var expectedCultures = new[] { "en", "fr", "es", "ja", "zh-Hans", "hi", "ar", "ru" };
    var actualCultures = Localization.SupportedLanguages.Select(language => language.CultureName).ToArray();

    Assert(
        expectedCultures.SequenceEqual(actualCultures, StringComparer.OrdinalIgnoreCase),
        $"Unexpected supported cultures: {string.Join(", ", actualCultures)}");

    foreach (var cultureName in expectedCultures)
    {
        var legacyBrandName = string.Concat("Privacy", (char)32, "Dot");

        foreach (AppText text in Enum.GetValues(typeof(AppText)))
        {
            var translation = Localization.GetForCulture(text, cultureName);
            Assert(
                !string.IsNullOrWhiteSpace(translation),
                $"Expected {text} translation for {cultureName}.");
            Assert(
                !translation.Contains(legacyBrandName),
                $"Expected compact PrivacyDot branding in {text} for {cultureName}.");
        }
    }
}

static void TestCultureResolution()
{
    AssertResolvedCulture("fr-CA", "fr");
    AssertResolvedCulture("es-MX", "es");
    AssertResolvedCulture("ja-JP", "ja");
    AssertResolvedCulture("zh-CN", "zh-Hans");
    AssertResolvedCulture("hi-IN", "hi");
    AssertResolvedCulture("ar-SA", "ar");
    AssertResolvedCulture("ru-RU", "ru");
    AssertResolvedCulture("de-DE", "en");
}

static void TestLocalizedSnapshotStatus()
{
    var previousCultureName = Localization.SelectedCultureName;

    try
    {
        Localization.SetLanguage("fr", persist: false);
        Assert(
            DeviceUsageSnapshot.Empty.StatusText == "Aucune utilisation du microphone ou de la caméra détectée",
            $"Unexpected French status: {DeviceUsageSnapshot.Empty.StatusText}");

        Localization.SetLanguage("ja", persist: false);
        Assert(
            DeviceUsageSnapshot.Empty.StatusText == "マイクまたはカメラの使用は検出されていません",
            $"Unexpected Japanese status: {DeviceUsageSnapshot.Empty.StatusText}");
    }
    finally
    {
        Localization.SetLanguage(previousCultureName, persist: false);
    }
}

static void TestArabicReadingDirection()
{
    var previousCultureName = Localization.SelectedCultureName;

    try
    {
        Localization.SetLanguage("ar", persist: false);
        Assert(Localization.IsRightToLeft, "Expected Arabic to use right-to-left layout.");
    }
    finally
    {
        Localization.SetLanguage(previousCultureName, persist: false);
    }
}

static void TestLocalizedPopup()
{
    RunOnStaThread(() =>
    {
        var previousCultureName = Localization.SelectedCultureName;

        try
        {
            Localization.SetLanguage("fr", persist: false);

            using (var frenchPopup = new UsagePopupForm(DeviceUsageSnapshot.Empty))
            {
                var frenchLabels = GetLabelTexts(frenchPopup);
                Assert(frenchLabels.Contains("Caméra"), "Expected the French camera heading.");
                Assert(
                    frenchLabels.Contains("Aucune application n’utilise actuellement cet appareil."),
                    "Expected the French empty-device message.");
                Assert(!frenchPopup.RightToLeftLayout, "Expected French to use left-to-right layout.");
            }

            Localization.SetLanguage("ar", persist: false);

            using (var arabicPopup = new UsagePopupForm(DeviceUsageSnapshot.Empty))
            {
                var arabicLabels = GetLabelTexts(arabicPopup);
                Assert(arabicLabels.Contains("الميكروفون"), "Expected the Arabic microphone heading.");
                Assert(
                    arabicLabels.Contains("لا توجد تطبيقات تستخدم هذا الجهاز حالياً."),
                    "Expected the Arabic empty-device message.");
                Assert(arabicPopup.RightToLeftLayout, "Expected the Arabic popup to mirror its layout.");
                Assert(arabicPopup.RightToLeft == RightToLeft.Yes, "Expected Arabic right-to-left text flow.");
            }
        }
        finally
        {
            Localization.SetLanguage(previousCultureName, persist: false);
        }
    });
}

static void TestNewerReleaseAvailable()
{
    var digest = new string('a', 64);
    var json = BuildReleaseJson("v0.2.0", digest, assetSize: 1234);
    var result = UpdateService.ParseLatestRelease(json, new Version(0, 1, 2));

    Assert(result.LatestVersion == new Version(0, 2, 0), "Expected v0.2.0 as the latest version.");
    Assert(result.AvailableUpdate is not null, "Expected a newer release to be available.");
    Assert(result.AvailableUpdate!.AssetSize == 1234, "Expected the release asset size.");
    Assert(result.AvailableUpdate.ExpectedSha256 == digest, "Expected the release SHA-256 digest.");
    Assert(
        result.AvailableUpdate.DownloadUri.AbsoluteUri.EndsWith("/PrivacyDotSetup.exe", StringComparison.Ordinal),
        "Expected the PrivacyDot installer download URL.");
}

static void TestInstalledReleaseIsCurrent()
{
    const string json =
        "{\"tag_name\":\"v0.1.2\",\"html_url\":\"https://github.com/farflashgroup/PrivacyDot/releases/tag/v0.1.2\",\"draft\":false,\"prerelease\":false,\"assets\":[]}";
    var result = UpdateService.ParseLatestRelease(json, new Version(0, 1, 2, 0));

    Assert(result.AvailableUpdate is null, "Expected the installed release to be current.");
}

static void TestUntrustedUpdateUrl()
{
    var json = BuildReleaseJson(
        "v0.2.0",
        new string('b', 64),
        assetSize: 1234,
        downloadUrl: "https://example.com/PrivacyDotSetup.exe");

    AssertThrows<InvalidDataException>(
        () => UpdateService.ParseLatestRelease(json, new Version(0, 1, 2)),
        "Expected a non-GitHub installer URL to be rejected.");

    var otherRepositoryJson = BuildReleaseJson(
        "v0.2.0",
        new string('c', 64),
        assetSize: 1234,
        downloadUrl: "https://github.com/example/OtherProject/releases/download/v0.2.0/PrivacyDotSetup.exe");

    AssertThrows<InvalidDataException>(
        () => UpdateService.ParseLatestRelease(otherRepositoryJson, new Version(0, 1, 2)),
        "Expected an installer from another GitHub repository to be rejected.");

    var wrongAssetJson = BuildReleaseJson(
        "v0.2.0",
        new string('d', 64),
        assetSize: 1234,
        downloadUrl: "https://github.com/farflashgroup/PrivacyDot/releases/download/v0.2.0/OtherSetup.exe");

    AssertThrows<InvalidDataException>(
        () => UpdateService.ParseLatestRelease(wrongAssetJson, new Version(0, 1, 2)),
        "Expected a URL for a different release asset to be rejected.");

    var queryStringJson = BuildReleaseJson(
        "v0.2.0",
        new string('e', 64),
        assetSize: 1234,
        downloadUrl: "https://github.com/farflashgroup/PrivacyDot/releases/download/v0.2.0/PrivacyDotSetup.exe?redirect=untrusted");

    AssertThrows<InvalidDataException>(
        () => UpdateService.ParseLatestRelease(queryStringJson, new Version(0, 1, 2)),
        "Expected a release asset URL with a query string to be rejected.");
}

static void TestOversizedReleaseMetadata()
{
    var oversizedResponse = new byte[UpdateService.MaximumReleaseMetadataSize + 1];
    var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new ByteArrayContent(oversizedResponse)
    });

    using var service = new UpdateService(handler);
    AssertThrows<InvalidDataException>(
        () => service.CheckForUpdatesAsync(new Version(0, 1, 2)).GetAwaiter().GetResult(),
        "Expected oversized GitHub release metadata to be rejected.");
}

static void TestInstallerStreamSizeLimit()
{
    using var source = new MemoryStream(new byte[16]);
    using var destination = new MemoryStream();

    AssertThrows<InvalidDataException>(
        () => UpdateService
            .CopyWithSizeLimitAsync(source, destination, maximumSize: 8, CancellationToken.None)
            .GetAwaiter()
            .GetResult(),
        "Expected installer data beyond the release size to be rejected.");
    Assert(destination.Length <= 8, "Expected the size-limited copy to cap bytes written to disk.");
}

static void TestVerifiedInstallerDownload()
{
    var installerBytes = Encoding.UTF8.GetBytes("deterministic fake PrivacyDot installer");
    string digest;

    using (var sha256 = SHA256.Create())
    {
        digest = BitConverter.ToString(sha256.ComputeHash(installerBytes)).Replace("-", string.Empty);
    }

    var releaseJson = BuildReleaseJson("v0.2.0", digest, installerBytes.Length);
    var handler = new StubHttpMessageHandler(request =>
    {
        if (request.RequestUri?.AbsoluteUri == UpdateService.LatestReleaseApiUrl)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(releaseJson, Encoding.UTF8, "application/json")
            };
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(installerBytes)
        };
    });

    using var service = new UpdateService(handler);
    var result = service.CheckForUpdatesAsync(new Version(0, 1, 2)).GetAwaiter().GetResult();
    Assert(result.AvailableUpdate is not null, "Expected an available update from the stubbed GitHub response.");
    Assert(handler.LastUserAgent?.StartsWith("PrivacyDot/", StringComparison.Ordinal) == true, "Expected a PrivacyDot User-Agent.");
    Assert(handler.LastApiVersion == "2022-11-28", "Expected the GitHub API version header.");

    var installerPath = service
        .DownloadInstallerAsync(result.AvailableUpdate!)
        .GetAwaiter()
        .GetResult();

    try
    {
        Assert(File.Exists(installerPath), "Expected the verified installer file.");
        Assert(File.ReadAllBytes(installerPath).SequenceEqual(installerBytes), "Unexpected installer bytes.");
        var zoneIdentifier = UpdateService.ReadInternetZoneIdentifier(installerPath);
        Assert(zoneIdentifier.Contains("ZoneId=3"), "Expected the installer to retain Internet-zone provenance.");
    }
    finally
    {
        File.Delete(installerPath);
    }
}

static void TestSettingsMenuStructure()
{
    RunOnStaThread(() =>
    {
        var previousCultureName = Localization.SelectedCultureName;

        try
        {
            Localization.SetLanguage("en", persist: false);

            using var context = new TrayApplicationContext();
            var topLevelItems = context.MenuForTesting.Items.OfType<ToolStripMenuItem>().ToArray();
            var settingsItem = topLevelItems.Single(item => item.Text == "Settings");
            var settingsChildren = settingsItem.DropDownItems.OfType<ToolStripMenuItem>().ToArray();

            Assert(topLevelItems.All(item => item.Text != "Language"), "Expected Language to move out of the top-level menu.");
            Assert(settingsChildren.Any(item => item.Text == "Start with Windows"), "Expected startup in Settings.");
            Assert(settingsChildren.Any(item => item.Text == "Language"), "Expected Language in Settings.");
            Assert(settingsChildren.Any(item => item.Text == "Check for updates"), "Expected updater in Settings.");

            context.SetAvailableUpdateForTesting(new Version(0, 2, 0));
            settingsChildren = settingsItem.DropDownItems.OfType<ToolStripMenuItem>().ToArray();
            var updateNote = settingsChildren.Single(item => item.Text == "Update available: PrivacyDot 0.2.0");
            Assert(!updateNote.Enabled, "Expected the update note to be non-interactive.");
            Assert(updateNote.Available, "Expected the update note to be available when an update is available.");
            Assert(
                settingsItem.DropDownItems.IndexOf(updateNote) == settingsItem.DropDownItems.Count - 1,
                "Expected the update note at the bottom of Settings.");

            context.SetAvailableUpdateForTesting(null);
            Assert(!updateNote.Available, "Expected the update note to be hidden when no update is available.");
        }
        finally
        {
            Localization.SetLanguage(previousCultureName, persist: false);
        }
    });
}

static string BuildReleaseJson(
    string tagName,
    string digest,
    long assetSize,
    string downloadUrl = "https://github.com/farflashgroup/PrivacyDot/releases/download/v0.2.0/PrivacyDotSetup.exe")
{
    return "{"
        + $"\"tag_name\":\"{tagName}\","
        + $"\"html_url\":\"https://github.com/farflashgroup/PrivacyDot/releases/tag/{tagName}\","
        + "\"draft\":false,\"prerelease\":false,\"assets\":[{"
        + $"\"name\":\"PrivacyDotSetup.exe\",\"state\":\"uploaded\",\"browser_download_url\":\"{downloadUrl}\","
        + $"\"digest\":\"sha256:{digest}\",\"size\":{assetSize}"
        + "}]}";
}

static void RenderDocumentationScreenshots(string outputDirectory)
{
    Directory.CreateDirectory(outputDirectory);
    var snapshot = DeviceUsageSnapshot.FromEntries(new[]
    {
        new DeviceUsageEntry(DeviceKind.Microphone, "Browser Tab", "Sample.BrowserTab", UsageSource.CoreAudio),
        new DeviceUsageEntry(DeviceKind.Microphone, "Video Call", "Sample.VideoCall", UsageSource.PrivacyRegistry),
        new DeviceUsageEntry(DeviceKind.Camera, "Camera App", "Sample.CameraApp", UsageSource.PrivacyRegistry)
    });

    RenderPopupScreenshot(snapshot, ThemePalette.Dark, Path.Combine(outputDirectory, "popup-dark.png"));
    RenderPopupScreenshot(snapshot, ThemePalette.Light, Path.Combine(outputDirectory, "popup-light.png"));
}

static void RenderPopupScreenshot(DeviceUsageSnapshot snapshot, ThemePalette theme, string outputPath)
{
    using var popup = new UsagePopupForm(snapshot, theme, closeOnDeactivate: false);
    popup.Location = new System.Drawing.Point(-10000, -10000);
    popup.Show();
    Application.DoEvents();
    popup.PerformLayout();
    popup.Refresh();

    using var bitmap = new System.Drawing.Bitmap(
        popup.ClientSize.Width,
        popup.ClientSize.Height,
        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
    popup.DrawToBitmap(bitmap, new System.Drawing.Rectangle(System.Drawing.Point.Empty, popup.ClientSize));
    bitmap.Save(outputPath, ImageFormat.Png);
    popup.Hide();
}

static void AssertThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

static void RunOnStaThread(Action action)
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            failure = ex;
        }
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();

    if (failure is not null)
    {
        throw new InvalidOperationException("STA test failed.", failure);
    }
}

static IReadOnlyList<string> GetLabelTexts(Control parent)
{
    var labels = new List<string>();

    foreach (Control child in parent.Controls)
    {
        if (child is Label label)
        {
            labels.Add(label.Text);
        }

        labels.AddRange(GetLabelTexts(child));
    }

    return labels;
}

static void AssertResolvedCulture(string requestedCultureName, string expectedCultureName)
{
    var actualCultureName = Localization.ResolveSupportedCultureName(
        CultureInfo.GetCultureInfo(requestedCultureName));
    Assert(
        string.Equals(actualCultureName, expectedCultureName, StringComparison.OrdinalIgnoreCase),
        $"Expected {requestedCultureName} to resolve to {expectedCultureName}, got {actualCultureName}.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    public string? LastUserAgent { get; private set; }

    public string? LastApiVersion { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastUserAgent = request.Headers.UserAgent.ToString();
        LastApiVersion = request.Headers.TryGetValues("X-GitHub-Api-Version", out var values)
            ? values.SingleOrDefault()
            : null;
        return Task.FromResult(_responseFactory(request));
    }
}
