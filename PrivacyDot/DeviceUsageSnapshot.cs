using System.Text;

namespace PrivacyDot;

internal sealed class DeviceUsageSnapshot : IEquatable<DeviceUsageSnapshot>
{
    public static readonly DeviceUsageSnapshot Empty = FromEntries(Array.Empty<DeviceUsageEntry>());

    private DeviceUsageSnapshot(
        IReadOnlyList<DeviceUsageEntry> microphoneApps,
        IReadOnlyList<DeviceUsageEntry> cameraApps,
        string fingerprint)
    {
        MicrophoneApps = microphoneApps;
        CameraApps = cameraApps;
        Fingerprint = fingerprint;
    }

    public IReadOnlyList<DeviceUsageEntry> MicrophoneApps { get; }

    public IReadOnlyList<DeviceUsageEntry> CameraApps { get; }

    public bool IsMicrophoneActive => MicrophoneApps.Count > 0;

    public bool IsCameraActive => CameraApps.Count > 0;

    public string Fingerprint { get; }

    public string StatusText
    {
        get
        {
            if (IsMicrophoneActive && IsCameraActive)
            {
                return "Microphone and camera in use";
            }

            if (IsMicrophoneActive)
            {
                return "Microphone in use";
            }

            if (IsCameraActive)
            {
                return "Camera in use";
            }

            return "No microphone or camera use detected";
        }
    }

    public string ToolTipText
    {
        get
        {
            var text = $"Privacy Dot: {StatusText}";
            return text.Length <= 127 ? text : text.Substring(0, 124) + "...";
        }
    }

    public static DeviceUsageSnapshot FromEntries(IEnumerable<DeviceUsageEntry> entries)
    {
        var uniqueEntries = entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.DisplayName) && !string.IsNullOrWhiteSpace(entry.Identity))
            .GroupBy(entry => $"{entry.Kind}|{entry.Identity}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderBy(entry => entry.Source == UsageSource.CoreAudio ? 0 : 1)
                .ThenBy(entry => entry.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .First())
            .OrderBy(entry => entry.Kind)
            .ThenBy(entry => entry.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => entry.Identity, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var microphoneApps = uniqueEntries
            .Where(entry => entry.Kind == DeviceKind.Microphone)
            .ToArray();

        var cameraApps = uniqueEntries
            .Where(entry => entry.Kind == DeviceKind.Camera)
            .ToArray();

        return new DeviceUsageSnapshot(microphoneApps, cameraApps, BuildFingerprint(uniqueEntries));
    }

    public bool Equals(DeviceUsageSnapshot? other)
    {
        return other is not null && StringComparer.Ordinal.Equals(Fingerprint, other.Fingerprint);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DeviceUsageSnapshot);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Fingerprint);
    }

    private static string BuildFingerprint(IEnumerable<DeviceUsageEntry> entries)
    {
        var builder = new StringBuilder();

        foreach (var entry in entries)
        {
            builder
                .Append(entry.Kind)
                .Append('|')
                .Append(entry.Identity)
                .Append('|')
                .Append(entry.DisplayName)
                .Append(';');
        }

        return builder.ToString();
    }
}
