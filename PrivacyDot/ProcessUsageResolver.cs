using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

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
            var resolvedIdentity = ReadIdentity(processId) ?? SafeGetMainModulePath(process);
            var identity = string.IsNullOrWhiteSpace(resolvedIdentity)
                ? $"pid:{processId}:{processName}"
                : resolvedIdentity!;

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
            return string.IsNullOrWhiteSpace(process.ProcessName)
                ? Localization.Get(AppText.UnknownApp)
                : process.ProcessName;
        }
        catch
        {
            return Localization.Get(AppText.UnknownApp);
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

    private static string? ReadIdentity(int processId)
    {
        using var handle = OpenProcess(0x1000 /* PROCESS_QUERY_LIMITED_INFORMATION */, false, processId);
        if (handle.IsInvalid) return null;
        try
        {
            uint length = 0;
            if (GetPackageFamilyName(handle, ref length, null) == 122 && length > 0 && length <= 1024)
            {
                var family = new StringBuilder((int)length);
                if (GetPackageFamilyName(handle, ref length, family) == 0 && family.Length > 0) return family.ToString();
            }
        }
        catch (EntryPointNotFoundException) { /* Windows 7 has no package identities. */ }
        var size = 32768;
        var path = new StringBuilder(size);
        return QueryFullProcessImageName(handle, 0, path, ref size) ? path.ToString() : null;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint access, bool inherit, int processId);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(SafeProcessHandle process, uint flags, StringBuilder path, ref int size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetPackageFamilyName(SafeProcessHandle process, ref uint length, StringBuilder? name);
}
