namespace PrivacyDot;

internal sealed class ThemedToolStripRenderer : ToolStripProfessionalRenderer
{
    private readonly ThemePalette _theme;

    public ThemedToolStripRenderer(ThemePalette theme)
        : base(new ThemedColorTable(theme))
    {
        _theme = theme;
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(_theme.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(_theme.Separator);
        var y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var bounds = new Rectangle(e.ImageRectangle.Left + 3, e.ImageRectangle.Top + 3, 12, 12);

        using var fill = new SolidBrush(_theme.CheckMark);
        using var pen = new Pen(_theme.MenuBack, 2);

        e.Graphics.FillEllipse(fill, bounds);
        e.Graphics.DrawLines(
            pen,
            new[]
            {
                new Point(bounds.Left + 3, bounds.Top + 6),
                new Point(bounds.Left + 6, bounds.Top + 9),
                new Point(bounds.Left + 10, bounds.Top + 4)
            });
    }

    private sealed class ThemedColorTable : ProfessionalColorTable
    {
        private readonly ThemePalette _theme;

        public ThemedColorTable(ThemePalette theme)
        {
            _theme = theme;
            UseSystemColors = false;
        }

        public override Color ToolStripDropDownBackground => _theme.MenuBack;

        public override Color ImageMarginGradientBegin => _theme.MenuBack;

        public override Color ImageMarginGradientMiddle => _theme.MenuBack;

        public override Color ImageMarginGradientEnd => _theme.MenuBack;

        public override Color MenuBorder => _theme.Border;

        public override Color MenuItemBorder => _theme.MenuHover;

        public override Color MenuItemSelected => _theme.MenuHover;

        public override Color MenuItemSelectedGradientBegin => _theme.MenuHover;

        public override Color MenuItemSelectedGradientEnd => _theme.MenuHover;

        public override Color CheckBackground => _theme.MenuHover;

        public override Color CheckSelectedBackground => _theme.MenuHover;

        public override Color CheckPressedBackground => _theme.MenuHover;
    }
}
