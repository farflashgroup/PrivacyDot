using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace PrivacyDot;

internal interface IProcessActions
{
    IPreparedProcessQuit? PrepareQuit(DeviceUsageEntry entry);
}

internal interface IPreparedProcessQuit : IDisposable
{
    int Count { get; }
    bool Execute();
}

internal sealed class ProcessActions : IProcessActions
{
    public IPreparedProcessQuit? PrepareQuit(DeviceUsageEntry entry)
    {
        // A display name or an unverified PID is not enough to identify a target.
        if (entry.Identity.StartsWith("pid:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var handles = new List<SafeProcessHandle>();
        using var current = Process.GetCurrentProcess();
        using var identity = WindowsIdentity.GetCurrent();
        try
        {
            foreach (var process in Process.GetProcesses())
            {
                using (process)
                {
                    if (process.Id == current.Id)
                    {
                        continue;
                    }

                    // Hold the verified kernel handle throughout confirmation. Never look
                    // the PID up again: Windows could have reused it for another process.
                    var handle = OpenProcess(0x1000 | 0x0001 | 0x00100000, false, process.Id);
                    var keep = false;
                    try
                    {
                        if (!handle.IsInvalid
                            && IsCurrentUserSession(handle, identity.User, current.SessionId)
                            && MatchesIdentity(entry.Identity, ReadPath(handle),
                                AppNameResolver.LooksLikeWindowsPath(entry.Identity) ? null : ReadPackageFamily(handle))
                            && WaitForSingleObject(handle, 0) == 0x102)
                        {
                            handles.Add(handle);
                            keep = true;
                        }
                    }
                    finally
                    {
                        if (!keep) handle.Dispose();
                    }
                }
            }

            return handles.Count == 0 ? null : new PreparedQuit(handles);
        }
        catch
        {
            foreach (var handle in handles) handle.Dispose();
            throw;
        }
    }

    internal static bool MatchesIdentity(string identity, string? path, string? packageFamily)
    {
        if (string.IsNullOrWhiteSpace(identity)) return false;
        return AppNameResolver.LooksLikeWindowsPath(identity)
            ? string.Equals(identity.Replace('/', '\\'), path, StringComparison.OrdinalIgnoreCase)
            : string.Equals(identity, packageFamily, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCurrentUserSession(SafeProcessHandle process, SecurityIdentifier? user, int session)
    {
        if (user is null || !OpenProcessToken(process, 0x0008, out var token)) return false;
        using (token)
        using (var owner = new WindowsIdentity(token.DangerousGetHandle()))
        {
            return user.Equals(owner.User)
                && GetTokenInformation(token, 12 /* TokenSessionId */, out var tokenSession, sizeof(int), out _)
                && tokenSession == session;
        }
    }

    private static string? ReadPath(SafeProcessHandle process)
    {
        var size = 32768;
        var buffer = new StringBuilder(size);
        return QueryFullProcessImageName(process, 0, buffer, ref size) ? buffer.ToString() : null;
    }

    private static string? ReadPackageFamily(SafeProcessHandle process)
    {
        try
        {
            uint length = 0;
            if (GetPackageFamilyName(process, ref length, null) != 122 || length == 0 || length > 1024) return null;
            var buffer = new StringBuilder((int)length);
            return GetPackageFamilyName(process, ref length, buffer) == 0 ? buffer.ToString() : null;
        }
        catch (EntryPointNotFoundException)
        {
            return null; // Windows 7 has no packaged processes.
        }
    }

    private sealed class PreparedQuit : IPreparedProcessQuit
    {
        private readonly List<SafeProcessHandle> _handles;
        private bool _disposed;

        public PreparedQuit(List<SafeProcessHandle> handles) => _handles = handles;
        public int Count => _handles.Count;

        public bool Execute()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PreparedQuit));
            var succeeded = true;
            foreach (var handle in _handles)
            {
                // Already-exited targets are harmless. Do not acquire new targets here.
                if (WaitForSingleObject(handle, 0) == 0) continue;
                if (!TerminateProcess(handle, 1) && WaitForSingleObject(handle, 0) != 0) succeeded = false;
            }
            return succeeded;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var handle in _handles) handle.Dispose();
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint access, bool inherit, int processId);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(SafeProcessHandle process, uint flags, StringBuilder path, ref int size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetPackageFamilyName(SafeProcessHandle process, ref uint length, StringBuilder? name);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateProcess(SafeProcessHandle process, uint exitCode);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(SafeProcessHandle handle, uint milliseconds);
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(SafeProcessHandle process, uint access, out SafeAccessTokenHandle token);
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(SafeAccessTokenHandle token, int informationClass, out int information, int length, out int returnLength);
}
