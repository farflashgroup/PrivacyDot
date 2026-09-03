using System.Diagnostics;

namespace PrivacyDot;

internal enum ProcessPresence
{
    NotRunning,
    Running,
    Unknown
}

internal static class SlackStaleUsageGuard
{
    // Keep this exception deliberately narrow: never trust a display name, an
    // unversioned directory, or an uncertain process query to hide a warning.
    private static readonly TimeSpan MinimumStaleAge = TimeSpan.FromSeconds(30);

    public static bool ShouldSuppress(
        DeviceUsageEntry entry,
        object? lastUsedTimeStart,
        object? lastUsedTimeStop)
    {
        return ShouldSuppress(
            entry,
            lastUsedTimeStart,
            lastUsedTimeStop,
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DateTime.UtcNow,
            GetProcessPresence);
    }

    internal static bool ShouldSuppress(
        DeviceUsageEntry entry,
        object? lastUsedTimeStart,
        object? lastUsedTimeStop,
        string localApplicationDataPath,
        DateTime utcNow,
        Func<string, ProcessPresence> getProcessPresence)
    {
        if (entry.Kind != DeviceKind.Microphone
            || entry.Source != UsageSource.PrivacyRegistry
            || RegistryUsageParser.ConvertToInt64(lastUsedTimeStop) != 0
            || !IsVersionedSlackExecutable(entry.Identity, localApplicationDataPath)
            || !IsOldEnough(lastUsedTimeStart, utcNow))
        {
            return false;
        }

        try
        {
            return getProcessPresence(entry.Identity) == ProcessPresence.NotRunning;
        }
        catch
        {
            // An inability to prove that Slack is absent must never hide a warning.
            return false;
        }
    }

    internal static bool IsVersionedSlackExecutable(string identity, string localApplicationDataPath)
    {
        if (string.IsNullOrWhiteSpace(identity) || string.IsNullOrWhiteSpace(localApplicationDataPath))
        {
            return false;
        }

        try
        {
            var executablePath = Path.GetFullPath(identity);
            var executable = new FileInfo(executablePath);
            var versionDirectory = executable.Directory;
            var slackDirectory = versionDirectory?.Parent;
            var expectedSlackDirectory = Path.GetFullPath(Path.Combine(localApplicationDataPath, "slack"));

            if (!string.Equals(executable.Name, "slack.exe", StringComparison.OrdinalIgnoreCase)
                || versionDirectory is null
                || slackDirectory is null
                || !string.Equals(slackDirectory.FullName, expectedSlackDirectory, StringComparison.OrdinalIgnoreCase)
                || !versionDirectory.Name.StartsWith("app-", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var versionText = versionDirectory.Name.Substring("app-".Length);
            return Version.TryParse(versionText, out var version)
                && version.Major >= 0
                && version.Minor >= 0;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException)
        {
            return false;
        }
    }

    private static bool IsOldEnough(object? lastUsedTimeStart, DateTime utcNow)
    {
        var start = RegistryUsageParser.ConvertToInt64(lastUsedTimeStart);

        if (start <= 0)
        {
            return false;
        }

        try
        {
            var startedAt = DateTime.FromFileTimeUtc(start);
            var age = utcNow - startedAt;
            return age >= MinimumStaleAge;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static ProcessPresence GetProcessPresence(string expectedExecutablePath)
    {
        Process[] processes;

        try
        {
            processes = Process.GetProcessesByName("slack");
        }
        catch
        {
            return ProcessPresence.Unknown;
        }

        var encounteredUnknownPath = false;

        try
        {
            foreach (var process in processes)
            {
                try
                {
                    var processPath = process.MainModule?.FileName;

                    if (string.IsNullOrWhiteSpace(processPath))
                    {
                        encounteredUnknownPath = true;
                    }
                    else if (PathsEqual(processPath!, expectedExecutablePath))
                    {
                        return ProcessPresence.Running;
                    }
                }
                catch
                {
                    encounteredUnknownPath = true;
                }
            }
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }

        return encounteredUnknownPath ? ProcessPresence.Unknown : ProcessPresence.NotRunning;
    }

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(left),
                Path.GetFullPath(right),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException)
        {
            return false;
        }
    }
}
