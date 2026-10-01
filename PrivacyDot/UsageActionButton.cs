using System.Drawing.Drawing2D;

namespace PrivacyDot;

internal enum UsageActionGlyph { MicrophoneOff, CameraOff, Quit, Warning }

// Vector strokes keep the icons monochrome, crisp at any DPI, and independent
// of the installed emoji font. Button supplies keyboard and accessibility behavior.
internal sealed class UsageActionButton : Button
{
    private readonly ThemePalette _theme;
    private bool _hover;
    private Keys _pressedKey;

    public UsageActionButton(UsageActionGlyph glyph, ThemePalette theme, bool unavailable = false)
    {
        Glyph = glyph;
        _theme = theme;
        Unavailable = unavailable;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = theme.PanelBack;
        ForeColor = theme.SecondaryText;
        Cursor = Cursors.Hand;
        Margin = Padding.Empty;
        TabStop = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    internal UsageActionGlyph Glyph { get; }
    internal bool Unavailable { get; }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Enter or Keys.Space || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            _pressedKey = e.KeyCode;
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            var activate = _pressedKey == e.KeyCode;
            _pressedKey = Keys.None;
            e.Handled = true;
            if (activate) PerformClick(); // Holding a key cannot confirm a destructive action.
            return;
        }
        base.OnKeyUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(_hover || Focused ? _theme.MenuHover : _theme.PanelBack);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var scale = DeviceDpi / 96f;
        var state = graphics.Save();
        graphics.TranslateTransform((Width - 20 * scale) / 2, (Height - 20 * scale) / 2);
        graphics.ScaleTransform(scale, scale);
        var color = Glyph == UsageActionGlyph.Warning ? _theme.WarningText
            : Unavailable ? _theme.DisabledText : _theme.SecondaryText;
        using var pen = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        switch (Glyph)
        {
            case UsageActionGlyph.MicrophoneOff:
                graphics.DrawArc(pen, 7, 2, 6, 6, 180, 180);
                graphics.DrawLine(pen, 7, 5, 7, 9);
                graphics.DrawLine(pen, 13, 5, 13, 9);
                graphics.DrawArc(pen, 7, 6, 6, 6, 0, 180);
                graphics.DrawArc(pen, 4, 4, 12, 12, 0, 180);
                graphics.DrawLine(pen, 10, 16, 10, 18);
                graphics.DrawLine(pen, 7, 18, 13, 18);
                DrawSlash(graphics, pen);
                break;
            case UsageActionGlyph.CameraOff:
                graphics.DrawRectangle(pen, 2, 5, 11, 10);
                graphics.DrawLines(pen, new[] { new PointF(13, 8), new PointF(18, 5), new PointF(18, 15), new PointF(13, 12) });
                DrawSlash(graphics, pen);
                break;
            case UsageActionGlyph.Quit:
                graphics.DrawLine(pen, 5, 5, 15, 15);
                graphics.DrawLine(pen, 15, 5, 5, 15);
                break;
            case UsageActionGlyph.Warning:
                graphics.DrawPolygon(pen, new[] { new PointF(10, 2), new PointF(19, 18), new PointF(1, 18) });
                graphics.DrawLine(pen, 10, 7, 10, 11);
                graphics.DrawLine(pen, 10, 14, 10, 14.3f);
                break;
        }
        graphics.Restore(state);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(graphics, Rectangle.Inflate(ClientRectangle, -3, -3), color, _theme.PanelBack);
    }

    private void DrawSlash(Graphics graphics, Pen pen)
    {
        using var gap = new Pen(_hover || Focused ? _theme.MenuHover : _theme.PanelBack, 4);
        graphics.DrawLine(gap, 2, 2, 18, 18);
        graphics.DrawLine(pen, 2, 2, 18, 18);
    }
}
