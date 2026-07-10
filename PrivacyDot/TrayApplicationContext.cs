using System.ComponentModel;
using System.Diagnostics;

namespace PrivacyDot;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly UsageMonitor _monitor;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _startupItem;
    private UsagePopupForm? _popup;
    private Icon? _currentIcon;
    private ThemePalette _theme = ThemePalette.Current;
    private bool _disposed;

    public TrayApplicationContext()
    {
        _monitor = new UsageMonitor();
        _monitor.SnapshotChanged += HandleSnapshotChanged;

        _statusItem = new ToolStripMenuItem(DeviceUsageSnapshot.Empty.StatusText) { Enabled = false };
        _startupItem = new ToolStripMenuItem("Start with Windows");
        _startupItem.Click += (_, _) => ToggleStartup();

        _menu = BuildMenu();
        _menu.Opening += HandleMenuOpening;

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _menu,
            Text = DeviceUsageSnapshot.Empty.ToolTipText,
            Visible = true
        };

        _notifyIcon.MouseUp += HandleNotifyIconMouseUp;
        ApplySnapshot(DeviceUsageSnapshot.Empty);
        _monitor.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _monitor.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _menu.Dispose();
            _currentIcon?.Dispose();
            _popup?.Close();
            _popup?.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip
        {
            ShowImageMargin = true
        };

        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Refresh", null, (_, _) => RefreshAndUpdatePopup());
        menu.Items.Add("Open microphone privacy settings", null, (_, _) => OpenSettings("ms-settings:privacy-microphone"));
        menu.Items.Add("Open camera privacy settings", null, (_, _) => OpenSettings("ms-settings:privacy-webcam"));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());

        ApplyMenuTheme(menu);

        return menu;
    }

    private void HandleSnapshotChanged(object? sender, DeviceUsageSnapshot snapshot)
    {
        ApplySnapshot(snapshot);

        if (_popup is { IsDisposed: false, Visible: true })
        {
            _popup.UpdateSnapshot(snapshot);
        }
    }

    private void ApplySnapshot(DeviceUsageSnapshot snapshot)
    {
        var previousIcon = _currentIcon;
        _currentIcon = IconFactory.Create(snapshot);
        _notifyIcon.Icon = _currentIcon;
        previousIcon?.Dispose();

        _notifyIcon.Text = snapshot.ToolTipText;
        _statusItem.Text = snapshot.StatusText;
    }

    private void HandleNotifyIconMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ShowUsagePopup();
        }
    }

    private void HandleMenuOpening(object? sender, CancelEventArgs e)
    {
        _startupItem.Checked = StartupManager.IsEnabled();
        ApplyMenuTheme(_menu);
    }

    private void ApplyMenuTheme(ContextMenuStrip menu)
    {
        _theme = ThemePalette.Current;

        menu.BackColor = _theme.MenuBack;
        menu.ForeColor = _theme.PrimaryText;
        menu.Renderer = new ThemedToolStripRenderer(_theme);

        foreach (ToolStripItem item in menu.Items)
        {
            ApplyMenuItemTheme(item);
        }
    }

    private void ApplyMenuItemTheme(ToolStripItem item)
    {
        item.BackColor = _theme.MenuBack;
        item.ForeColor = item.Enabled ? _theme.PrimaryText : _theme.DisabledText;

        if (item is ToolStripMenuItem menuItem)
        {
            foreach (ToolStripItem child in menuItem.DropDownItems)
            {
                ApplyMenuItemTheme(child);
            }
        }
    }

    private void ShowUsagePopup()
    {
        RefreshAndUpdatePopup();
        _popup?.Close();

        _popup = new UsagePopupForm(_monitor.Current);
        _popup.FormClosed += (_, _) => _popup = null;
        _popup.ShowNearCursor();
    }

    private void RefreshAndUpdatePopup()
    {
        _monitor.RefreshNow();

        if (_popup is { IsDisposed: false, Visible: true })
        {
            _popup.UpdateSnapshot(_monitor.Current);
        }
    }

    private void ToggleStartup()
    {
        try
        {
            StartupManager.SetEnabled(!StartupManager.IsEnabled());
            _startupItem.Checked = StartupManager.IsEnabled();
        }
        catch (Exception ex)
        {
            ShowBalloon("Privacy Dot startup setting failed", ex.Message, ToolTipIcon.Error);
        }
    }

    private void OpenSettings(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ShowBalloon("Privacy settings failed to open", ex.Message, ToolTipIcon.Error);
        }
    }

    private void ShowBalloon(string title, string message, ToolTipIcon icon)
    {
        _notifyIcon.ShowBalloonTip(3000, title, message, icon);
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        ExitThread();
    }
}
