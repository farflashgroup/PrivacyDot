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

    public UsagePopupForm(DeviceUsageSnapshot snapshot)
    {
        _snapshot = snapshot;
        var baseFont = SystemFonts.MessageBoxFont ?? Control.DefaultFont;
        _titleFont = new Font(baseFont, FontStyle.Bold);
        _sectionFont = new Font(baseFont, FontStyle.Bold);
        _itemFont = new Font(baseFont, FontStyle.Regular);
        _detailFont = new Font(baseFont.FontFamily, Math.Max(7.5f, baseFont.Size - 1f));

        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = SystemColors.Window;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Padding = new Padding(1);

        _scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = SystemColors.Window
        };

        Controls.Add(_scrollHost);
        Rebuild();
    }

    public void UpdateSnapshot(DeviceUsageSnapshot snapshot)
    {
        _snapshot = snapshot;
        Rebuild();
    }

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
        Close();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var borderPen = new Pen(Color.FromArgb(170, SystemColors.ControlDark));
        e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _titleFont.Dispose();
            _sectionFont.Dispose();
            _itemFont.Dispose();
            _detailFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Rebuild()
    {
        _scrollHost.SuspendLayout();
        _scrollHost.Controls.Clear();

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = SystemColors.Window,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Padding = new Padding(14, 12, 14, 14)
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _scrollHost.Controls.Add(layout);

        layout.Controls.Add(CreateLabel("Privacy Dot", _titleFont, SystemColors.ControlText, 0));
        layout.Controls.Add(CreateLabel(_snapshot.StatusText, _detailFont, SystemColors.GrayText, 3));

        AddSpacer(layout, 10);
        AddSection(layout, "Microphone", Color.FromArgb(36, 211, 102), _snapshot.MicrophoneApps);
        AddSpacer(layout, 10);
        AddSection(layout, "Camera", Color.FromArgb(255, 149, 0), _snapshot.CameraApps);

        var preferred = layout.GetPreferredSize(new Size(PopupWidth - 2, 0));
        ClientSize = new Size(PopupWidth, Math.Min(MaxPopupHeight, preferred.Height + 2));
        _scrollHost.AutoScrollMinSize = new Size(PopupWidth - 2, preferred.Height);

        _scrollHost.ResumeLayout();
        Invalidate();
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
            Text = title
        };

        layout.Controls.Add(header);

        if (entries.Count == 0)
        {
            layout.Controls.Add(CreateLabel("No apps currently using this device.", _itemFont, SystemColors.GrayText, 0));
            return;
        }

        foreach (var entry in entries)
        {
            layout.Controls.Add(CreateLabel(entry.DisplayName, _itemFont, SystemColors.ControlText, 2));
            layout.Controls.Add(CreateLabel(CompactIdentity(entry), _detailFont, SystemColors.GrayText, 0));
        }
    }

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
            Text = text
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
