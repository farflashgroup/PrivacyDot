using PrivacyDot;

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
    ("Snapshot deduplicates registry and Core Audio", TestSnapshotDeduplicatesEntries)
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

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
