using System.Globalization;
using System.Text;

namespace PrivacyDot;

internal sealed class DeviceUsageSnapshot : IEquatable<DeviceUsageSnapshot>
{
    internal const int MaximumToolTipLength = 63;
    private const string ToolTipEllipsis = "…";

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
                return Localization.Get(AppText.StatusMicrophoneAndCameraInUse);
            }

            if (IsMicrophoneActive)
            {
                return Localization.Get(AppText.StatusMicrophoneInUse);
            }

            if (IsCameraActive)
            {
                return Localization.Get(AppText.StatusCameraInUse);
            }

            return Localization.Get(AppText.StatusNoDeviceUse);
        }
    }

    public string ToolTipText
    {
        get
        {
            var text = Localization.Format(AppText.ToolTipFormat, StatusText);
            return LimitToolTipText(text);
        }
    }

    internal static string LimitToolTipText(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (text.Length <= MaximumToolTipLength)
        {
            return text;
        }

        var contentLimit = MaximumToolTipLength - ToolTipEllipsis.Length;
        var textElementStarts = StringInfo.ParseCombiningCharacters(text);
        var contentLength = 0;

        for (var index = 0; index < textElementStarts.Length; index++)
        {
            var elementEnd = index + 1 < textElementStarts.Length
                ? textElementStarts[index + 1]
                : text.Length;

            if (elementEnd > contentLimit)
            {
                break;
            }

            contentLength = elementEnd;
        }

        return text.Substring(0, contentLength).TrimEnd() + ToolTipEllipsis;
    }

    public static DeviceUsageSnapshot FromEntries(IEnumerable<DeviceUsageEntry> entries)
    {
        var uniqueEntries = entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.DisplayName) && !string.IsNullOrWhiteSpace(entry.Identity))
            .GroupBy(entry => entry.AppKey, StringComparer.Ordinal)
            // Registry usage has no endpoint. Once this app has a confirmed device,
            // do not add a duplicate row under "Device not identified".
            .SelectMany(group => group.Any(entry => entry.DeviceId is not null)
                ? group.Where(entry => entry.DeviceId is not null) : group)
            .GroupBy(entry => entry.RowKey, StringComparer.Ordinal)
            .Select(group => group
                .OrderBy(entry => entry.Source == UsageSource.PrivacyRegistry ? 1 : 0)
                .ThenBy(entry => entry.DeviceName is null ? 1 : 0)
                .ThenBy(entry => entry.DisplayName, StringComparer.Ordinal)
                .First())
            .OrderBy(entry => entry.Kind)
            .ThenBy(entry => entry.DeviceId, StringComparer.OrdinalIgnoreCase)
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
                .Append('|')
                .Append(entry.DeviceId)
                .Append('|')
                .Append(entry.DeviceName)
                .Append('|')
                .Append(entry.Source)
                .Append(';');
        }

        return builder.ToString();
    }
}
