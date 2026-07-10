namespace PrivacyDot;

internal sealed class DeviceUsageEntry
{
    public DeviceUsageEntry(DeviceKind kind, string displayName, string identity, UsageSource source)
    {
        Kind = kind;
        DisplayName = displayName;
        Identity = identity;
        Source = source;
    }

    public DeviceKind Kind { get; }

    public string DisplayName { get; }

    public string Identity { get; }

    public UsageSource Source { get; }

    public string SourceLabel
    {
        get
        {
            return Source switch
            {
                UsageSource.CoreAudio => "Core Audio",
                UsageSource.PrivacyRegistry => "Windows Privacy",
                _ => Source.ToString()
            };
        }
    }

    public DeviceUsageEntry WithSource(UsageSource source)
    {
        return new DeviceUsageEntry(Kind, DisplayName, Identity, source);
    }
}
