using System.ComponentModel;
using System.Diagnostics;

namespace PrivacyDot;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly UsageMonitor _monitor;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _refreshItem;
    private readonly ToolStripMenuItem _microphoneSettingsItem;
    private readonly ToolStripMenuItem _cameraSettingsItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _languageItem;
    private readonly ToolStripMenuItem _systemLanguageItem;
    private readonly ToolStripMenuItem _checkForUpdatesItem;
    private readonly ToolStripSeparator _updateAvailableSeparator;
    private readonly ToolStripMenuItem _updateAvailableNoteItem;
    private readonly ToolStripMenuItem _exitItem;
    private readonly Dictionary<ToolStripMenuItem, string> _languageItems;
    private readonly UpdateService _updateService;
    private readonly CancellationTokenSource _shutdownCancellation = new();
    private UsagePopupForm? _popup;
    private Icon? _currentIcon;
    private ThemePalette _theme = ThemePalette.Current;
    private UpdateUiState _updateUiState;
    private Version? _availableUpdateVersion;
    private bool _disposed;

    public TrayApplicationContext()
    {
        _monitor = new UsageMonitor();
        _monitor.SnapshotChanged += HandleSnapshotChanged;
        _updateService = new UpdateService();

        _statusItem = new ToolStripMenuItem { Enabled = false };
        _refreshItem = new ToolStripMenuItem();
        _refreshItem.Click += (_, _) => RefreshAndUpdatePopup();
        _microphoneSettingsItem = new ToolStripMenuItem();
        _microphoneSettingsItem.Click += (_, _) => OpenSettings("ms-settings:privacy-microphone");
        _cameraSettingsItem = new ToolStripMenuItem();
        _cameraSettingsItem.Click += (_, _) => OpenSettings("ms-settings:privacy-webcam");
        _startupItem = new ToolStripMenuItem();
        _startupItem.Click += (_, _) => ToggleStartup();
        _settingsItem = new ToolStripMenuItem();
        _languageItem = new ToolStripMenuItem();
        _languageItem.DropDownOpening += HandleLanguageDropDownOpening;
        _systemLanguageItem = new ToolStripMenuItem();
        _systemLanguageItem.Click += (_, _) => ChangeLanguage(null);
        _languageItem.DropDownItems.Add(_systemLanguageItem);
        _languageItem.DropDownItems.Add(new ToolStripSeparator());
        _languageItems = new Dictionary<ToolStripMenuItem, string>();

        foreach (var language in Localization.SupportedLanguages)
        {
            var languageItem = new ToolStripMenuItem(language.NativeName);
            languageItem.Click += (_, _) => ChangeLanguage(language.CultureName);
            _languageItems.Add(languageItem, language.CultureName);
            _languageItem.DropDownItems.Add(languageItem);
        }

        _checkForUpdatesItem = new ToolStripMenuItem();
        _checkForUpdatesItem.Click += async (_, _) => await CheckForUpdatesAsync();
        _updateAvailableSeparator = new ToolStripSeparator { Visible = false };
        _updateAvailableNoteItem = new ToolStripMenuItem { Enabled = false, Visible = false };
        _settingsItem.DropDownItems.Add(_startupItem);
        _settingsItem.DropDownItems.Add(new ToolStripSeparator());
        _settingsItem.DropDownItems.Add(_languageItem);
        _settingsItem.DropDownItems.Add(new ToolStripSeparator());
        _settingsItem.DropDownItems.Add(_checkForUpdatesItem);
        _settingsItem.DropDownItems.Add(_updateAvailableSeparator);
        _settingsItem.DropDownItems.Add(_updateAvailableNoteItem);

        _exitItem = new ToolStripMenuItem();
        _exitItem.Click += (_, _) => ExitApplication();

        _menu = BuildMenu();
        _menu.Opening += HandleMenuOpening;

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _menu,
            Text = DeviceUsageSnapshot.Empty.ToolTipText,
            Visible = true
        };

        _notifyIcon.MouseUp += HandleNotifyIconMouseUp;
        ApplyLocalizedText(DeviceUsageSnapshot.Empty);
        ApplySnapshot(DeviceUsageSnapshot.Empty);
        _monitor.Start();
        Application.Idle += HandleFirstApplicationIdle;
    }

    internal ContextMenuStrip MenuForTesting => _menu;

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            Application.Idle -= HandleFirstApplicationIdle;
            _shutdownCancellation.Cancel();
            _monitor.Dispose();
            _updateService.Dispose();
            _shutdownCancellation.Dispose();
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
        menu.Items.Add(_refreshItem);
        menu.Items.Add(_microphoneSettingsItem);
        menu.Items.Add(_cameraSettingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_settingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_exitItem);

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
        ApplyLocalizedText(_monitor.Current);
        ApplyMenuTheme(_menu);
    }

    private void HandleLanguageDropDownOpening(object? sender, EventArgs e)
    {
        var owner = _languageItem.Owner;

        if (owner is null)
        {
            return;
        }

        var itemLocation = owner.PointToScreen(_languageItem.Bounds.Location);
        var itemBounds = new Rectangle(itemLocation, _languageItem.Bounds.Size);
        var workingArea = Screen.FromRectangle(itemBounds).WorkingArea;
        var dropDownSize = _languageItem.DropDown.GetPreferredSize(Size.Empty);
        _languageItem.DropDownDirection = ChooseLanguageDropDownDirection(
            itemBounds,
            dropDownSize,
            workingArea,
            Localization.IsRightToLeft);
    }

    internal static ToolStripDropDownDirection ChooseLanguageDropDownDirection(
        Rectangle itemBounds,
        Size dropDownSize,
        Rectangle workingArea,
        bool preferLeft)
    {
        var spaceOnLeft = Math.Max(0, itemBounds.Left - workingArea.Left);
        var spaceOnRight = Math.Max(0, workingArea.Right - itemBounds.Right);
        var fitsOnLeft = dropDownSize.Width <= spaceOnLeft;
        var fitsOnRight = dropDownSize.Width <= spaceOnRight;

        if (fitsOnLeft && fitsOnRight)
        {
            return preferLeft ? ToolStripDropDownDirection.Left : ToolStripDropDownDirection.Right;
        }

        if (fitsOnLeft)
        {
            return ToolStripDropDownDirection.Left;
        }

        if (fitsOnRight)
        {
            return ToolStripDropDownDirection.Right;
        }

        return spaceOnLeft > spaceOnRight
            ? ToolStripDropDownDirection.Left
            : ToolStripDropDownDirection.Right;
    }

    private void ApplyLocalizedText(DeviceUsageSnapshot snapshot)
    {
        _statusItem.Text = snapshot.StatusText;
        _refreshItem.Text = Localization.Get(AppText.Refresh);
        _microphoneSettingsItem.Text = Localization.Get(AppText.OpenMicrophonePrivacySettings);
        _cameraSettingsItem.Text = Localization.Get(AppText.OpenCameraPrivacySettings);
        _startupItem.Text = Localization.Get(AppText.StartWithWindows);
        _settingsItem.Text = Localization.Get(AppText.Settings);
        _languageItem.Text = Localization.Get(AppText.Language);
        _systemLanguageItem.Text = Localization.Get(AppText.SystemDefault);
        _checkForUpdatesItem.Text = Localization.Get(_updateUiState switch
        {
            UpdateUiState.Checking => AppText.CheckingForUpdates,
            UpdateUiState.Downloading => AppText.DownloadingUpdate,
            _ => AppText.CheckForUpdates
        });
        _checkForUpdatesItem.Enabled = _updateUiState == UpdateUiState.Idle;
        var updateAvailable = _availableUpdateVersion is not null;
        _updateAvailableSeparator.Visible = updateAvailable;
        _updateAvailableNoteItem.Visible = updateAvailable;
        _updateAvailableNoteItem.Text = updateAvailable
            ? Localization.Format(
                AppText.UpdateAvailableNoteFormat,
                UpdateService.FormatVersion(_availableUpdateVersion!))
            : string.Empty;
        _exitItem.Text = Localization.Get(AppText.Exit);
        _systemLanguageItem.Checked = Localization.SelectedCultureName is null;

        foreach (var languageItem in _languageItems)
        {
            languageItem.Key.Checked = string.Equals(
                languageItem.Value,
                Localization.SelectedCultureName,
                StringComparison.OrdinalIgnoreCase);
        }

        var direction = Localization.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No;
        _menu.RightToLeft = direction;
        _settingsItem.DropDown.RightToLeft = direction;
        _languageItem.DropDown.RightToLeft = direction;
        _notifyIcon.Text = snapshot.ToolTipText;
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

    private void ChangeLanguage(string? cultureName)
    {
        Localization.SetLanguage(cultureName);
        ApplyLocalizedText(_monitor.Current);
        ApplyMenuTheme(_menu);

        if (_popup is { IsDisposed: false, Visible: true })
        {
            _popup.UpdateSnapshot(_monitor.Current);
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        if (_updateUiState != UpdateUiState.Idle)
        {
            return;
        }

        SetUpdateUiState(UpdateUiState.Checking);

        try
        {
            var result = await _updateService.CheckForUpdatesAsync(
                cancellationToken: _shutdownCancellation.Token);
            SetAvailableUpdate(result.AvailableUpdate?.Version);

            if (result.AvailableUpdate is null)
            {
                ShowUpdateMessage(
                    Localization.Format(
                        AppText.UpToDateFormat,
                        UpdateService.FormatVersion(UpdateService.InstalledVersion)),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var update = result.AvailableUpdate;
            var choice = ShowUpdateMessage(
                Localization.Format(
                    AppText.UpdateAvailableFormat,
                    UpdateService.FormatVersion(update.Version),
                    UpdateService.FormatVersion(UpdateService.InstalledVersion)),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (choice != DialogResult.Yes)
            {
                return;
            }

            SetUpdateUiState(UpdateUiState.Downloading);
            var installerPath = await _updateService.DownloadInstallerAsync(
                update,
                _shutdownCancellation.Token);

            Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
            ExitApplication();
        }
        catch (OperationCanceledException) when (_shutdownCancellation.IsCancellationRequested)
        {
            // Application shutdown cancels an in-progress update cleanly.
        }
        catch (Exception ex)
        {
            if (!_disposed)
            {
                ShowUpdateMessage(
                    Localization.Format(AppText.UpdateFailedFormat, ex.Message),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        finally
        {
            if (!_disposed)
            {
                SetUpdateUiState(UpdateUiState.Idle);
            }
        }
    }

    private async void HandleFirstApplicationIdle(object? sender, EventArgs e)
    {
        Application.Idle -= HandleFirstApplicationIdle;

        if (_disposed || _updateUiState != UpdateUiState.Idle)
        {
            return;
        }

        SetUpdateUiState(UpdateUiState.Checking);

        try
        {
            var result = await _updateService.CheckForUpdatesAsync(
                cancellationToken: _shutdownCancellation.Token);
            SetAvailableUpdate(result.AvailableUpdate?.Version);
        }
        catch (OperationCanceledException) when (_shutdownCancellation.IsCancellationRequested)
        {
            // Application shutdown cancels the quiet startup check cleanly.
        }
        catch
        {
            // A quiet startup check should never interrupt normal app use.
        }
        finally
        {
            if (!_disposed)
            {
                SetUpdateUiState(UpdateUiState.Idle);
            }
        }
    }

    internal void SetAvailableUpdateForTesting(Version? version)
    {
        SetAvailableUpdate(version);
    }

    private void SetAvailableUpdate(Version? version)
    {
        _availableUpdateVersion = version;
        ApplyLocalizedText(_monitor.Current);
        ApplyMenuTheme(_menu);
    }

    private void SetUpdateUiState(UpdateUiState state)
    {
        _updateUiState = state;
        ApplyLocalizedText(_monitor.Current);
        ApplyMenuTheme(_menu);
    }

    private static DialogResult ShowUpdateMessage(
        string message,
        MessageBoxButtons buttons,
        MessageBoxIcon icon)
    {
        var options = Localization.IsRightToLeft
            ? MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading
            : 0;

        return MessageBox.Show(
            message,
            Localization.Get(AppText.AppName),
            buttons,
            icon,
            MessageBoxDefaultButton.Button1,
            options);
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
            ShowBalloon(Localization.Get(AppText.StartupSettingFailed), ex.Message, ToolTipIcon.Error);
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
            ShowBalloon(Localization.Get(AppText.PrivacySettingsOpenFailed), ex.Message, ToolTipIcon.Error);
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

    private enum UpdateUiState
    {
        Idle,
        Checking,
        Downloading
    }
}
