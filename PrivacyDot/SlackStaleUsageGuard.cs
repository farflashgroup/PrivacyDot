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
    // A process launched after an old record cannot have started that capture.
    private static readonly TimeSpan MinimumStaleAge = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ProcessStartTolerance = TimeSpan.FromSeconds(30);

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
        Func<string, DateTime, ProcessPresence> getProcessPresence)
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
            var usageStartedUtc = DateTime.FromFileTimeUtc(RegistryUsageParser.ConvertToInt64(lastUsedTimeStart));
            return getProcessPresence(entry.Identity, usageStartedUtc) == ProcessPresence.NotRunning;
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

    private static ProcessPresence GetProcessPresence(string expectedExecutablePath, DateTime usageStartedUtc)
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

        var observations = new List<(string? Path, DateTime? StartedUtc)>();

        try
        {
            foreach (var process in processes)
            {
                try
                {
                    observations.Add((process.MainModule?.FileName, process.StartTime.ToUniversalTime()));
                }
                catch
                {
                    observations.Add((null, null));
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

        return GetProcessPresence(expectedExecutablePath, usageStartedUtc, observations);
    }

    internal static ProcessPresence GetProcessPresence(
        string expectedExecutablePath,
        DateTime usageStartedUtc,
        IEnumerable<(string? Path, DateTime? StartedUtc)> processes)
    {
        var encounteredUnknownProcess = false;
        foreach (var process in processes)
        {
            if (string.IsNullOrWhiteSpace(process.Path))
            {
                encounteredUnknownProcess = true;
            }
            else if (PathsEqual(process.Path!, expectedExecutablePath))
            {
                if (process.StartedUtc is null)
                    encounteredUnknownProcess = true;
                else if (process.StartedUtc.Value - usageStartedUtc <= ProcessStartTolerance)
                    return ProcessPresence.Running;
            }
        }

        return encounteredUnknownProcess ? ProcessPresence.Unknown : ProcessPresence.NotRunning;
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
