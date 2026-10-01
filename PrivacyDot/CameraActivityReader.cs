using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PrivacyDot;

// Reports metadata only. No camera source is opened and no frames are captured.
internal sealed class CameraActivityReader : IDisposable
{
    private readonly CameraActivityCache _cache = new();
    private readonly SensorActivitiesCallback _callback;
    private IMFSensorActivityMonitor? _monitor;
    private bool _mfStarted;
    private bool _started;
    private bool _disposed;
    private long _reportsReceived;

    public CameraActivityReader() => _callback = new SensorActivitiesCallback(ReceiveReport);
    internal bool IsAvailable { get; private set; }
    internal long ReportsReceived => Interlocked.Read(ref _reportsReceived);

    public void Start()
    {
        if (_started || _disposed) return;
        _started = true;
        try
        {
            if (MFStartup(0x00020070, 1 /* MFSTARTUP_NOSOCKET */) < 0) return;
            _mfStarted = true;
            if (MFCreateSensorActivityMonitor(_callback, out _monitor) >= 0 && _monitor is not null)
                IsAvailable = _monitor.Start() >= 0;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or COMException)
        {
            // Windows 7/8 and Windows N may lack this API. Registry fallback remains.
        }
    }

    public IReadOnlyList<DeviceUsageEntry> GetActiveCameraApps() => _cache.GetActiveEntries(IsSameProcess);

    private static bool IsSameProcess(CameraProcessUsage usage)
    {
        try
        {
            using var process = Process.GetProcessById(usage.ProcessId);
            return !process.HasExited && process.StartTime.ToUniversalTime().Ticks == usage.ProcessStartUtcTicks;
        }
        catch { return false; }
    }

    private void ReceiveReport(IMFSensorActivitiesReport report)
    {
        var revision = Interlocked.Increment(ref _reportsReceived);
        var updates = new List<CameraDeviceReport>();
        if (report.GetCount(out var count) < 0) return;
        for (uint i = 0; i < Math.Min(count, 256); i++)
        {
            IMFSensorActivityReport? device = null;
            try
            {
                if (report.GetActivityReport(i, out device) < 0 || device is null) continue;
                var id = new StringBuilder(32768);
                if (device.GetSymbolicLink(id, (uint)id.Capacity, out _) < 0 || id.Length == 0) continue;
                var name = new StringBuilder(1024);
                var friendlyName = device.GetFriendlyName(name, (uint)name.Capacity, out _) >= 0 ? name.ToString() : null;
                if (device.GetProcessCount(out var processCount) < 0) continue;
                var processes = new List<CameraProcessUsage>();
                for (uint j = 0; j < Math.Min(processCount, 4096); j++)
                {
                    IMFSensorProcessActivity? activity = null;
                    try
                    {
                        if (device.GetProcessActivity(j, out activity) < 0 || activity is null
                            || activity.GetStreamingState(out var streaming) < 0 || !streaming
                            || activity.GetProcessId(out var pid) < 0 || pid == 0 || pid > int.MaxValue) continue;
                        using var process = Process.GetProcessById((int)pid);
                        var started = process.StartTime.ToUniversalTime();
                        if (activity.GetReportTime(out var reportTime) >= 0 && reportTime > 0
                            && started.ToFileTimeUtc() > reportTime) continue;
                        var entry = ProcessUsageResolver.TryCreateFromProcessId((int)pid, DeviceKind.Camera, UsageSource.CameraActivity);
                        if (entry is not null && !process.HasExited)
                            processes.Add(new CameraProcessUsage((int)pid, started.Ticks, entry.WithDevice(id.ToString(), friendlyName)));
                    }
                    catch { /* A client may exit between the report and the lookup. */ }
                    finally { Release(activity); }
                }
                updates.Add(new CameraDeviceReport(id.ToString(), processes));
            }
            catch { /* A removed device must not discard reports for other cameras. */ }
            finally { Release(device); }
        }
        _cache.ApplyReports(updates, revision);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cache.Dispose();
        // Never hold the cache lock while stopping: callbacks may be in flight.
        try { _monitor?.Stop(); }
        catch (COMException) { }
        finally
        {
            Release(_monitor);
            _monitor = null;
            if (_mfStarted) MFShutdown();
            _mfStarted = false;
            IsAvailable = false;
            GC.KeepAlive(_callback);
        }
    }

    internal static void Release(object? value)
    {
        if (value is not null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value);
    }

    [DllImport("mfplat.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int MFStartup(uint version, uint flags);
    [DllImport("mfplat.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int MFShutdown();
    [DllImport("mfsensorgroup.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int MFCreateSensorActivityMonitor(ISensorActivitiesCallback callback, out IMFSensorActivityMonitor monitor);
}

// The callback is explicitly COM-visible; everything behind it remains internal.
[ComVisible(true), Guid("DE5072EE-DBE3-46DC-8A87-B6F631194751"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ISensorActivitiesCallback
{
    [PreserveSig] int OnActivitiesReport(IntPtr report);
}

[ComVisible(true), ClassInterface(ClassInterfaceType.None)]
public sealed class SensorActivitiesCallback : ISensorActivitiesCallback
{
    private readonly Action<IMFSensorActivitiesReport> _receive;
    internal SensorActivitiesCallback(Action<IMFSensorActivitiesReport> receive) => _receive = receive;

    public int OnActivitiesReport(IntPtr pointer)
    {
        object? report = null;
        try
        {
            if (pointer != IntPtr.Zero)
            {
                report = Marshal.GetObjectForIUnknown(pointer);
                _receive((IMFSensorActivitiesReport)report);
            }
        }
        catch { /* Never propagate managed exceptions across the native callback. */ }
        finally { CameraActivityReader.Release(report); }
        return 0;
    }
}

[ComImport, Guid("D0CEF145-B3F4-4340-A2E5-7A5080CA05CB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFSensorActivityMonitor
{
    [PreserveSig] int Start();
    [PreserveSig] int Stop();
}

[ComImport, Guid("683F7A5E-4A19-43CD-B1A9-DBF4AB3F7777"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFSensorActivitiesReport
{
    [PreserveSig] int GetCount(out uint count);
    [PreserveSig] int GetActivityReport(uint index, out IMFSensorActivityReport report);
    [PreserveSig] int GetActivityReportByDeviceName([MarshalAs(UnmanagedType.LPWStr)] string name, out IMFSensorActivityReport report);
}

[ComImport, Guid("3E8C4BE1-A8C2-4528-90DE-2851BDE5FEAD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFSensorActivityReport
{
    [PreserveSig] int GetFriendlyName([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder name, uint capacity, out uint written);
    [PreserveSig] int GetSymbolicLink([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder link, uint capacity, out uint written);
    [PreserveSig] int GetProcessCount(out uint count);
    [PreserveSig] int GetProcessActivity(uint index, out IMFSensorProcessActivity activity);
}

[ComImport, Guid("39DC7F4A-B141-4719-813C-A7F46162A2B8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFSensorProcessActivity
{
    [PreserveSig] int GetProcessId(out uint processId);
    [PreserveSig] int GetStreamingState([MarshalAs(UnmanagedType.Bool)] out bool streaming);
    [PreserveSig] int GetStreamingMode(out int mode);
    [PreserveSig] int GetReportTime(out long fileTime);
}
