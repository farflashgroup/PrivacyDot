using Microsoft.Win32;
using System.Security;

namespace PrivacyDot;

internal sealed class RegistryUsageReader
{
    private const string ConsentStorePath = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore";
    private const int MaximumTraversalDepth = 8;
    private const int MaximumSubKeysPerCapability = 4096;

    public IReadOnlyList<DeviceUsageEntry> GetActiveUsage()
    {
        var entries = new List<DeviceUsageEntry>();

        ReadCapability(entries, DeviceKind.Microphone, "microphone");
        ReadCapability(entries, DeviceKind.Camera, "webcam");

        return entries;
    }

    private static void ReadCapability(List<DeviceUsageEntry> entries, DeviceKind kind, string capabilityName)
    {
        try
        {
            using var capabilityKey = Registry.CurrentUser.OpenSubKey($@"{ConsentStorePath}\{capabilityName}");

            if (capabilityKey is null)
            {
                return;
            }

            var inspectedSubKeys = 0;
            ReadSubKeys(entries, kind, capabilityKey, depth: 0, ref inspectedSubKeys);
        }
        catch (Exception ex) when (ex is IOException or SecurityException or UnauthorizedAccessException)
        {
            // Privacy records are best-effort; the tray indicator should keep running.
        }
    }

    private static void ReadSubKeys(
        List<DeviceUsageEntry> entries,
        DeviceKind kind,
        RegistryKey parentKey,
        int depth,
        ref int inspectedSubKeys)
    {
        if (depth >= MaximumTraversalDepth || inspectedSubKeys >= MaximumSubKeysPerCapability)
        {
            return;
        }

        foreach (var subKeyName in parentKey.GetSubKeyNames())
        {
            if (inspectedSubKeys >= MaximumSubKeysPerCapability)
            {
                return;
            }

            inspectedSubKeys++;

            try
            {
                using var subKey = parentKey.OpenSubKey(subKeyName);

                if (subKey is null)
                {
                    continue;
                }

                var lastUsedTimeStart = subKey.GetValue("LastUsedTimeStart");
                var lastUsedTimeStop = subKey.GetValue("LastUsedTimeStop");

                if (RegistryUsageParser.TryCreateEntry(
                    kind,
                    subKeyName,
                    lastUsedTimeStart,
                    lastUsedTimeStop,
                    out var entry)
                    && entry is not null
                    && !SlackStaleUsageGuard.ShouldSuppress(entry, lastUsedTimeStart, lastUsedTimeStop))
                {
                    entries.Add(entry);
                }

                ReadSubKeys(entries, kind, subKey, depth + 1, ref inspectedSubKeys);
            }
            catch (Exception ex) when (ex is IOException or SecurityException or UnauthorizedAccessException)
            {
                // Continue reading other app entries if one entry is unavailable.
            }
        }
    }
}
