namespace WarcArchiver.App.Theme;

/// <summary>
/// Draws StatusStrip / ToolStrip surfaces in the dark palette instead of the system colours. Item text keeps the
/// item's own ForeColor (the status bar uses it for green / amber / red messages).
/// </summary>
internal sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
{
    public DarkToolStripRenderer()
        : base(new DarkColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(DarkTheme.Surface);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(DarkTheme.Border);
        e.Graphics.DrawLine(pen, 0, 0, e.ToolStrip.Width, 0);
    }

    sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => DarkTheme.Surface;

        public override Color ToolStripGradientMiddle => DarkTheme.Surface;

        public override Color ToolStripGradientEnd => DarkTheme.Surface;

        public override Color StatusStripGradientBegin => DarkTheme.Surface;

        public override Color StatusStripGradientEnd => DarkTheme.Surface;

        public override Color ToolStripBorder => DarkTheme.Border;

        public override Color SeparatorDark => DarkTheme.Border;

        public override Color SeparatorLight => DarkTheme.Border;
    }
}
