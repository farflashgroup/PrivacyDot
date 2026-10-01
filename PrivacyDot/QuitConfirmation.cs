using System.Diagnostics;

namespace PrivacyDot;

internal enum QuitResult { Armed, TooSoon, Completed, Unavailable, Failed }

internal sealed class QuitConfirmation : IDisposable
{
    private readonly IProcessActions _actions;
    private readonly Func<double> _seconds;
    private IPreparedProcessQuit? _request;
    private double _armedAt;
    private string? _key;

    public QuitConfirmation(IProcessActions actions, Func<double>? seconds = null)
    {
        _actions = actions;
        _seconds = seconds ?? (() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
    }

    public int ProcessCount => _request?.Count ?? 0;
    public bool IsArmed(DeviceUsageEntry entry) => _request is not null && _key == Key(entry);
    public bool CanConfirm => _request is not null && _seconds() - _armedAt >= MinimumDelay;
    private static double MinimumDelay => Math.Max(0.75, SystemInformation.DoubleClickTime / 1000d + 0.2);

    public QuitResult Click(DeviceUsageEntry entry)
    {
        Expire();
        if (IsArmed(entry))
        {
            if (!CanConfirm) return QuitResult.TooSoon;
            try { return _request!.Execute() ? QuitResult.Completed : QuitResult.Failed; }
            catch { return QuitResult.Failed; }
            finally { Cancel(); }
        }

        Cancel();
        try
        {
            _request = _actions.PrepareQuit(entry);
            if (_request is null) return QuitResult.Unavailable;
            _key = Key(entry);
            _armedAt = _seconds();
            return QuitResult.Armed;
        }
        catch
        {
            Cancel();
            return QuitResult.Failed;
        }
    }

    public bool Expire()
    {
        if (_request is null || _seconds() - _armedAt < 10) return false;
        Cancel();
        return true;
    }

    public void Cancel()
    {
        _request?.Dispose();
        _request = null;
        _key = null;
    }

    public void Dispose() => Cancel();
    private static string Key(DeviceUsageEntry entry) => entry.RowKey;
}
