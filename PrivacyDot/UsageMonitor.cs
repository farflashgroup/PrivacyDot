namespace PrivacyDot;

internal sealed class UsageMonitor : IDisposable
{
    private readonly RegistryUsageReader _registryUsageReader = new();
    private readonly CoreAudioUsageReader _coreAudioUsageReader = new();
    private readonly System.Windows.Forms.Timer _timer;
    private bool _disposed;

    public UsageMonitor()
    {
        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += (_, _) => RefreshNow();
    }

    public event EventHandler<DeviceUsageSnapshot>? SnapshotChanged;

    public DeviceUsageSnapshot Current { get; private set; } = DeviceUsageSnapshot.Empty;

    public void Start()
    {
        RefreshNow();
        _timer.Start();
    }

    public void RefreshNow()
    {
        if (_disposed)
        {
            return;
        }

        var snapshot = CaptureSnapshot();

        if (!snapshot.Equals(Current))
        {
            Current = snapshot;
            SnapshotChanged?.Invoke(this, snapshot);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Dispose();
    }

    private DeviceUsageSnapshot CaptureSnapshot()
    {
        var entries = new List<DeviceUsageEntry>();

        entries.AddRange(_registryUsageReader.GetActiveUsage());
        entries.AddRange(_coreAudioUsageReader.GetActiveMicrophoneApps());

        return DeviceUsageSnapshot.FromEntries(entries);
    }
}
