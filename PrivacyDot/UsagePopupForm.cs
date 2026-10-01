using System.Drawing.Drawing2D;

namespace PrivacyDot;

internal sealed class UsagePopupForm : Form
{
    private const int PopupWidth = 390;
    private const int MaxPopupHeight = 520;
    private readonly Panel _scrollHost;
    private readonly Font _titleFont;
    private readonly Font _sectionFont;
    private readonly Font _itemFont;
    private readonly Font _detailFont;
    private DeviceUsageSnapshot _snapshot;
    private readonly ThemePalette? _themeOverride;
    private readonly bool _closeOnDeactivate;
    private ThemePalette _theme;
    private readonly ToolTip _toolTip = new();
    private readonly QuitConfirmation _quit;
    private readonly System.Windows.Forms.Timer _confirmationTimer;
    private string? _message;
    private string? _messageKey;
    private bool _closing;
    private readonly DeviceNameRegistry _deviceNames;

    public UsagePopupForm(
        DeviceUsageSnapshot snapshot,
        ThemePalette? themeOverride = null,
        bool closeOnDeactivate = true,
        IProcessActions? processActions = null,
        Func<double>? seconds = null,
        DeviceNameRegistry? deviceNames = null)
    {
        _snapshot = snapshot;
        _themeOverride = themeOverride;
        _closeOnDeactivate = closeOnDeactivate;
        _theme = ResolveTheme();
        _deviceNames = deviceNames ?? new DeviceNameRegistry();
        _quit = new QuitConfirmation(processActions ?? new ProcessActions(), seconds);
        _confirmationTimer = new System.Windows.Forms.Timer { Interval = 200 };
        _confirmationTimer.Tick += (_, _) =>
        {
            if (_quit.Expire()) Rebuild();
        };
        _confirmationTimer.Start();
        var baseFont = SystemFonts.MessageBoxFont ?? Control.DefaultFont;
        _titleFont = new Font(baseFont, FontStyle.Bold);
        _sectionFont = new Font(baseFont, FontStyle.Bold);
        _itemFont = new Font(baseFont, FontStyle.Regular);
        _detailFont = new Font(baseFont.FontFamily, Math.Max(7.5f, baseFont.Size - 1f));

        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Padding = new Padding(1);

        _scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        Controls.Add(_scrollHost);
        ApplyTheme();
        Rebuild();
    }

    public void UpdateSnapshot(DeviceUsageSnapshot snapshot)
    {
        // Changes to attribution require a fresh confirmation, including a row
        // disappearing and reappearing for a newly launched process.
        _quit.Cancel();
        _message = null;
        _snapshot = snapshot;
        Rebuild();
    }

    public event EventHandler? ProcessQuit;

    public void ShowNearCursor()
    {
        var cursor = Cursor.Position;
        var workingArea = Screen.FromPoint(cursor).WorkingArea;
        var x = Clamp(cursor.X - Width / 2, workingArea.Left + 8, workingArea.Right - Width - 8);
        var y = cursor.Y - Height - 12;

        if (y < workingArea.Top + 8)
        {
            y = Clamp(cursor.Y + 12, workingArea.Top + 8, workingArea.Bottom - Height - 8);
        }

        Location = new Point(x, y);
        Show();
        Activate();
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        if (_closing || Disposing || IsDisposed) return;
        _quit.Cancel();

        if (_closeOnDeactivate)
        {
            Close();
        }
        else if (!IsDisposed) Rebuild();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        _closing = !e.Cancel;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            if (_quit.ProcessCount > 0 || _message is not null)
            {
                _quit.Cancel();
                _message = null;
                Rebuild();
                return true;
            }
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var borderPen = new Pen(_theme.Border);
        e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _closing = true;
            _confirmationTimer.Dispose();
            _quit.Dispose();
            _toolTip.Dispose();
        }

        // Child controls can still receive layout/paint/deactivation messages
        // while the native window is destroyed. Keep their fonts alive until
        // those controls are disposed, and never rebuild during shutdown.
        base.Dispose(disposing);

        if (disposing)
        {
            _titleFont.Dispose();
            _sectionFont.Dispose();
            _itemFont.Dispose();
            _detailFont.Dispose();
        }
    }

    private void Rebuild()
    {
        if (_closing || Disposing || IsDisposed) return;
        _theme = ResolveTheme();
        ApplyTheme();
        ApplyReadingDirection();
        _scrollHost.SuspendLayout();
        var scrollPosition = _scrollHost.AutoScrollPosition;
        _toolTip.RemoveAll();
        while (_scrollHost.Controls.Count > 0) _scrollHost.Controls[0].Dispose();

        var popupWidth = ScalePixels(PopupWidth);

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = _theme.PanelBack,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Padding = new Padding(ScalePixels(14), ScalePixels(12), ScalePixels(14), ScalePixels(14)),
            RightToLeft = Localization.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _scrollHost.Controls.Add(layout);

        layout.Controls.Add(CreateLabel(Localization.Get(AppText.AppName), _titleFont, _theme.PrimaryText, 0));
        layout.Controls.Add(CreateLabel(_snapshot.StatusText, _detailFont, _theme.SecondaryText, 3));

        AddSpacer(layout, 10);
        AddSection(
            layout,
            Localization.Get(AppText.Microphone),
            Color.FromArgb(36, 211, 102),
            _snapshot.MicrophoneApps);
        AddSpacer(layout, 10);
        AddSection(
            layout,
            Localization.Get(AppText.Camera),
            Color.FromArgb(255, 149, 0),
            _snapshot.CameraApps);

        var preferred = layout.GetPreferredSize(new Size(popupWidth - 2, 0));
        ClientSize = new Size(popupWidth, Math.Min(ScalePixels(MaxPopupHeight), preferred.Height + 2));
        _scrollHost.AutoScrollMinSize = new Size(0, preferred.Height);

        _scrollHost.ResumeLayout();
        _scrollHost.AutoScrollPosition = new Point(-scrollPosition.X, -scrollPosition.Y);
        if (Visible && _closeOnDeactivate)
        {
            var area = Screen.FromControl(this).WorkingArea;
            Location = new Point(Clamp(Left, area.Left, area.Right - Width), Clamp(Top, area.Top, area.Bottom - Height));
        }
        Invalidate();
    }

    private void ApplyTheme()
    {
        BackColor = _theme.WindowBack;
        _scrollHost.BackColor = _theme.PanelBack;
    }

    private void ApplyReadingDirection()
    {
        var direction = Localization.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No;
        RightToLeft = direction;
        RightToLeftLayout = Localization.IsRightToLeft;
        _scrollHost.RightToLeft = direction;
    }

    private ThemePalette ResolveTheme()
    {
        return _themeOverride ?? ThemePalette.Current;
    }

    private void AddSection(
        TableLayoutPanel layout,
        string title,
        Color accentColor,
        IReadOnlyList<DeviceUsageEntry> entries)
    {
        var header = new Label
        {
            AutoSize = true,
            Font = _sectionFont,
            ForeColor = accentColor,
            Margin = new Padding(0, 0, 0, 4),
            MaximumSize = new Size(PopupWidth - 32, 0),
            RightToLeft = Localization.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No,
            Text = title
        };

        layout.Controls.Add(header);

        if (entries.Count == 0)
        {
            layout.Controls.Add(CreateLabel(
                Localization.Get(AppText.NoAppsUsingDevice),
                _itemFont,
                _theme.SecondaryText,
                0));
            return;
        }

        foreach (var group in DeviceUsageGroup.FromEntries(entries, _deviceNames))
        {
            var deviceLabel = CreateLabel(group.Name, _sectionFont, _theme.PrimaryText, 4);
            deviceLabel.Margin = new Padding(0, ScalePixels(4), 0, ScalePixels(3));
            deviceLabel.MaximumSize = new Size(ScalePixels(PopupWidth - 52), 0);
            _toolTip.SetToolTip(deviceLabel, group.DeviceId is null
                ? Localization.Get(AppText.DeviceNotIdentifiedExplanation) : $"{group.Name}\n{group.DeviceId}");
            layout.Controls.Add(deviceLabel);
            if (group.DeviceId is null)
            {
                layout.Controls.Add(CreateLabel(Localization.Get(AppText.DeviceNotIdentifiedExplanation),
                    _detailFont, _theme.SecondaryText, 0));
            }
            foreach (var entry in group.Apps) layout.Controls.Add(CreateEntryRow(entry));
        }
    }

    private Control CreateEntryRow(DeviceUsageEntry entry)
    {
        var row = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            Margin = new Padding(0, ScalePixels(2), 0, ScalePixels(4)),
            // Keep the requested actions on the right; text retains its language direction.
            RightToLeft = RightToLeft.No
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ScalePixels(64)));
        var identity = CompactIdentity(entry);
        var text = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Margin = new Padding(0, 0, ScalePixels(8), 0),
            RightToLeft = Localization.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No
        };
        text.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var nameLabel = CreateEntryLabel(entry.DisplayName, _itemFont, _theme.PrimaryText);
        var identityLabel = CreateEntryLabel(identity, _detailFont, _theme.SecondaryText);
        // Executable paths have a fixed left-to-right syntax in every UI language.
        if (AppNameResolver.LooksLikeWindowsPath(entry.Identity)) identityLabel.RightToLeft = RightToLeft.No;
        foreach (var label in new[] { nameLabel, identityLabel })
        {
            text.Controls.Add(label);
            _toolTip.SetToolTip(label, label.Text);
        }
        row.Controls.Add(text, 0, 0);
        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = Padding.Empty,
            Dock = DockStyle.Fill,
            RightToLeft = RightToLeft.No
        };
        row.Controls.Add(actions, 1, 0);
        if (_quit.IsArmed(entry))
        {
            var warning = CreateAction(UsageActionGlyph.Warning, Localization.Get(AppText.ConfirmForceQuit), false, 64);
            warning.Click += (_, _) => HandleQuit(entry);
            actions.Controls.Add(warning);
            AddRowMessage(row, Localization.Format(AppText.ForceQuitWarningFormat, entry.DisplayName, _quit.ProcessCount));
        }
        else
        {
            var unavailable = Localization.Get(entry.Kind == DeviceKind.Microphone
                ? AppText.MicrophoneControlUnavailable : AppText.CameraControlUnavailable);
            var device = CreateAction(entry.Kind == DeviceKind.Microphone
                ? UsageActionGlyph.MicrophoneOff : UsageActionGlyph.CameraOff, unavailable, true);
            device.AccessibleDescription = Localization.Get(AppText.DeviceControlExplanation);
            _toolTip.SetToolTip(device, unavailable + "\n" + device.AccessibleDescription);
            // An unavailable action stays focusable so keyboard and assistive-tech
            // users can discover the explanation, just as pointer users can.
            device.Click += (_, _) =>
            {
                _quit.Cancel();
                _messageKey = EntryKey(entry);
                _message = Localization.Get(AppText.DeviceControlExplanation);
                Rebuild();
            };
            actions.Controls.Add(device);
            var quit = CreateAction(UsageActionGlyph.Quit, Localization.Format(AppText.ForceQuitFormat, entry.DisplayName));
            quit.Click += (_, _) => HandleQuit(entry);
            actions.Controls.Add(quit);
            if (_message is not null && _messageKey == EntryKey(entry)) AddRowMessage(row, _message);
        }
        return row;
    }

    private Label CreateEntryLabel(string text, Font font, Color color) => new()
    {
        AutoSize = false,
        AutoEllipsis = true,
        Dock = DockStyle.Top,
        Height = TextRenderer.MeasureText("Ag", font).Height + ScalePixels(1),
        Margin = Padding.Empty,
        Text = text,
        UseMnemonic = false,
        Font = font,
        ForeColor = color,
        RightToLeft = Localization.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No
    };

    private UsageActionButton CreateAction(UsageActionGlyph glyph, string label, bool unavailable = false, int width = 32)
    {
        var button = new UsageActionButton(glyph, _theme, unavailable)
        {
            Size = new Size(ScalePixels(width), ScalePixels(32)),
            AccessibleName = label,
            AccessibleDescription = label
        };
        _toolTip.SetToolTip(button, label);
        return button;
    }

    private void AddRowMessage(TableLayoutPanel row, string message)
    {
        var label = CreateLabel(message, _detailFont, _theme.WarningText, 4);
        label.MaximumSize = new Size(ScalePixels(PopupWidth - 52), 0);
        label.Dock = DockStyle.Top;
        row.Controls.Add(label, 0, 1);
        row.SetColumnSpan(label, 2);
    }

    private void HandleQuit(DeviceUsageEntry entry)
    {
        _message = null;
        var result = _quit.Click(entry);
        if (result == QuitResult.TooSoon) return;
        _messageKey = EntryKey(entry);
        _message = result switch
        {
            QuitResult.Unavailable => Localization.Get(AppText.ForceQuitUnavailable),
            QuitResult.Failed => Localization.Get(AppText.ForceQuitFailed),
            QuitResult.Completed => Localization.Get(AppText.ForceQuitCompleted),
            _ => null
        };
        Rebuild();
        if (result == QuitResult.Armed)
        {
            FindWarning(this)?.Focus();
        }
        if (result is QuitResult.Completed or QuitResult.Failed) ProcessQuit?.Invoke(this, EventArgs.Empty);
    }

    private static UsageActionButton? FindWarning(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is UsageActionButton { Glyph: UsageActionGlyph.Warning } button) return button;
            var found = FindWarning(child);
            if (found is not null) return found;
        }
        return null;
    }

    private int ScalePixels(int value) => (int)Math.Round(value * DeviceDpi / 96d);
    private static string EntryKey(DeviceUsageEntry entry) => entry.RowKey;

    private static void AddSpacer(TableLayoutPanel layout, int height)
    {
        layout.Controls.Add(new Panel
        {
            Height = height,
            Margin = Padding.Empty
        });
    }

    private static Label CreateLabel(string text, Font font, Color color, int topMargin)
    {
        return new Label
        {
            AutoSize = true,
            Font = font,
            ForeColor = color,
            Margin = new Padding(0, topMargin, 0, 0),
            MaximumSize = new Size(PopupWidth - 32, 0),
            RightToLeft = Localization.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No,
            Text = text,
            UseMnemonic = false
        };
    }

    private static string CompactIdentity(DeviceUsageEntry entry)
    {
        if (AppNameResolver.LooksLikeWindowsPath(entry.Identity))
        {
            return entry.Identity;
        }

        return $"{entry.Identity} - {entry.SourceLabel}";
    }

    private static int Clamp(int value, int min, int max)
    {
        if (max < min)
        {
            return min;
        }

        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }
}
