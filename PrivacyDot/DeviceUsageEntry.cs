namespace PrivacyDot;

internal sealed class DeviceUsageEntry
{
    public DeviceUsageEntry(DeviceKind kind, string displayName, string identity, UsageSource source,
        string? deviceId = null, string? deviceName = null)
    {
        Kind = kind;
        DisplayName = displayName;
        Identity = identity;
        Source = source;
        DeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId;
        DeviceName = DeviceId is null || string.IsNullOrWhiteSpace(deviceName) ? null : deviceName;
    }

    public DeviceKind Kind { get; }

    public string DisplayName { get; }

    public string Identity { get; }

    public UsageSource Source { get; }

    public string? DeviceId { get; }
    public string? DeviceName { get; }
    internal string AppKey => $"{Kind}:{Identity.Length}:{Identity.ToUpperInvariant()}";
    internal string RowKey => $"{AppKey}:{DeviceId?.ToUpperInvariant()}";

    public string SourceLabel
    {
        get
        {
            return Source switch
            {
                UsageSource.CoreAudio => Localization.Get(AppText.CoreAudio),
                UsageSource.PrivacyRegistry => Localization.Get(AppText.WindowsPrivacy),
                UsageSource.CameraActivity => Localization.Get(AppText.CameraActivity),
                _ => Source.ToString()
            };
        }
    }

    public DeviceUsageEntry WithSource(UsageSource source)
    {
        return new DeviceUsageEntry(Kind, DisplayName, Identity, source, DeviceId, DeviceName);
    }

    public DeviceUsageEntry WithDevice(string? id, string? name) => new(Kind, DisplayName, Identity, Source, id, name);
}
