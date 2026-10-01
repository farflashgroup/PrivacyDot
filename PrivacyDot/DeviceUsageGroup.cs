namespace PrivacyDot;

internal sealed class DeviceUsageGroup
{
    private DeviceUsageGroup(string? id, string name, IReadOnlyList<DeviceUsageEntry> apps)
    {
        DeviceId = id;
        Name = name;
        Apps = apps;
    }

    public string? DeviceId { get; }
    public string Name { get; private set; }
    public IReadOnlyList<DeviceUsageEntry> Apps { get; }

    public static IReadOnlyList<DeviceUsageGroup> FromEntries(IReadOnlyList<DeviceUsageEntry> entries, DeviceNameRegistry? names = null)
    {
        var groups = entries.GroupBy(entry => entry.DeviceId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DeviceUsageGroup(group.Key,
                group.Key is null ? Localization.Get(AppText.DeviceNotIdentified)
                    : group.Select(entry => entry.DeviceName).FirstOrDefault(name => name is not null)
                        ?? Localization.Get(group.First().Kind == DeviceKind.Microphone ? AppText.Microphone : AppText.Camera),
                group.ToArray()))
            .OrderBy(group => group.DeviceId is null ? 1 : 0)
            .ThenBy(group => group.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(group => group.DeviceId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        names ??= new DeviceNameRegistry();
        foreach (var group in groups.Where(group => group.DeviceId is not null)) names.Register(group.Name, group.DeviceId!);
        foreach (var group in groups.Where(group => group.DeviceId is not null))
        {
            group.Name = names.GetLabel(group.Name, group.DeviceId!);
        }
        return groups;
    }
}

// Owned by the tray context, so labels survive refreshes and reopening the popup.
// A number is never reassigned when another identical device stops streaming.
internal sealed class DeviceNameRegistry
{
    private readonly Dictionary<string, Dictionary<string, int>> _names = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, string id)
    {
        if (!_names.TryGetValue(name, out var devices))
            _names[name] = devices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (!devices.ContainsKey(id)) devices[id] = devices.Count + 1;
    }

    public string GetLabel(string name, string id)
    {
        var devices = _names[name];
        return devices.Count == 1 ? name : $"{name} ({devices[id]})";
    }
}
