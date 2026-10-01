namespace PrivacyDot;

internal sealed class CameraProcessUsage
{
    public CameraProcessUsage(int processId, long processStartUtcTicks, DeviceUsageEntry entry)
    {
        ProcessId = processId;
        ProcessStartUtcTicks = processStartUtcTicks;
        Entry = entry;
    }
    public int ProcessId { get; }
    public long ProcessStartUtcTicks { get; }
    public DeviceUsageEntry Entry { get; }
}

internal sealed class CameraDeviceReport
{
    public CameraDeviceReport(string deviceId, IReadOnlyList<CameraProcessUsage> processes)
    {
        DeviceId = deviceId;
        Processes = processes;
    }
    public string DeviceId { get; }
    public IReadOnlyList<CameraProcessUsage> Processes { get; }
}

internal sealed class CameraActivityCache : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, CameraProcessUsage[]> _devices = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _revisions = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public void ApplyReports(IEnumerable<CameraDeviceReport> reports, long revision)
    {
        lock (_gate)
        {
            if (_disposed) return;
            // Notifications update the devices they mention, not every device.
            // Multiple reports for one sensor can disagree within a notification;
            // preserve every confirmed streaming client in that batch.
            foreach (var group in reports.GroupBy(report => report.DeviceId, StringComparer.OrdinalIgnoreCase))
            {
                if (_revisions.TryGetValue(group.Key, out var previous) && previous >= revision) continue;
                if (!_revisions.ContainsKey(group.Key) && _revisions.Count >= 256) continue;
                _revisions[group.Key] = revision;
                var active = group.SelectMany(report => report.Processes)
                    .GroupBy(process => (process.ProcessId, process.ProcessStartUtcTicks))
                    .Select(process => process.First()).ToArray();
                if (active.Length == 0) _devices.Remove(group.Key);
                else if (_devices.ContainsKey(group.Key) || _devices.Count < 256) _devices[group.Key] = active;
            }
        }
    }

    public IReadOnlyList<DeviceUsageEntry> GetActiveEntries(Func<CameraProcessUsage, bool> isSameProcess)
    {
        CameraProcessUsage[] processes;
        lock (_gate) processes = _devices.Values.SelectMany(value => value).ToArray();
        return processes.Where(isSameProcess).Select(process => process.Entry).ToArray();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _devices.Clear();
            _revisions.Clear();
        }
    }
}
