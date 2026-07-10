namespace PrivacyDot;

internal static class RegistryUsageParser
{
    public static bool TryCreateEntry(
        DeviceKind kind,
        string registryKeyName,
        object? lastUsedTimeStart,
        object? lastUsedTimeStop,
        out DeviceUsageEntry? entry)
    {
        entry = null;

        if (!IsActive(lastUsedTimeStart, lastUsedTimeStop))
        {
            return false;
        }

        var identity = AppNameResolver.DecodeRegistryIdentity(registryKeyName);

        if (string.Equals(identity, "NonPackaged", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        entry = new DeviceUsageEntry(
            kind,
            AppNameResolver.ResolveDisplayName(identity),
            identity,
            UsageSource.PrivacyRegistry);

        return true;
    }

    public static bool IsActive(object? lastUsedTimeStart, object? lastUsedTimeStop)
    {
        var start = ConvertToInt64(lastUsedTimeStart);
        var stop = ConvertToInt64(lastUsedTimeStop);

        return start > 0 && (stop == 0 || stop < start);
    }

    internal static long ConvertToInt64(object? value)
    {
        return value switch
        {
            null => 0,
            long longValue => longValue,
            int intValue => intValue,
            uint uintValue => uintValue,
            ulong ulongValue when ulongValue <= long.MaxValue => (long)ulongValue,
            string stringValue when long.TryParse(stringValue, out var parsed) => parsed,
            byte[] bytes when bytes.Length >= sizeof(long) => BitConverter.ToInt64(bytes, 0),
            _ => 0
        };
    }
}
