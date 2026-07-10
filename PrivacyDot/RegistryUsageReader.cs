using Microsoft.Win32;
using System.Security;

namespace PrivacyDot;

internal sealed class RegistryUsageReader
{
    private const string ConsentStorePath = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore";

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

            ReadSubKeys(entries, kind, capabilityKey);
        }
        catch (Exception ex) when (ex is IOException or SecurityException or UnauthorizedAccessException)
        {
            // Privacy records are best-effort; the tray indicator should keep running.
        }
    }

    private static void ReadSubKeys(List<DeviceUsageEntry> entries, DeviceKind kind, RegistryKey parentKey)
    {
        foreach (var subKeyName in parentKey.GetSubKeyNames())
        {
            try
            {
                using var subKey = parentKey.OpenSubKey(subKeyName);

                if (subKey is null)
                {
                    continue;
                }

                if (RegistryUsageParser.TryCreateEntry(
                    kind,
                    subKeyName,
                    subKey.GetValue("LastUsedTimeStart"),
                    subKey.GetValue("LastUsedTimeStop"),
                    out var entry)
                    && entry is not null)
                {
                    entries.Add(entry);
                }

                ReadSubKeys(entries, kind, subKey);
            }
            catch (Exception ex) when (ex is IOException or SecurityException or UnauthorizedAccessException)
            {
                // Continue reading other app entries if one entry is unavailable.
            }
        }
    }
}
