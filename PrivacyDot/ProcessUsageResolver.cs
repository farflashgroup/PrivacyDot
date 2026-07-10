using System.Diagnostics;

namespace PrivacyDot;

internal static class ProcessUsageResolver
{
    public static DeviceUsageEntry? TryCreateFromProcessId(int processId, DeviceKind kind, UsageSource source)
    {
        if (processId <= 0)
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            var processName = SafeGetProcessName(process);
            var path = SafeGetMainModulePath(process);
            var identity = string.IsNullOrWhiteSpace(path)
                ? $"pid:{processId}:{processName}"
                : path!;

            return new DeviceUsageEntry(
                kind,
                AppNameResolver.ResolveDisplayName(identity, processName),
                identity,
                source);
        }
        catch
        {
            return null;
        }
    }

    private static string SafeGetProcessName(Process process)
    {
        try
        {
            return string.IsNullOrWhiteSpace(process.ProcessName) ? "Unknown app" : process.ProcessName;
        }
        catch
        {
            return "Unknown app";
        }
    }

    private static string? SafeGetMainModulePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }
}
