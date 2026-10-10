using System.ComponentModel;
using System.Drawing.Drawing2D;
using WarcArchiver.App.Theme;

namespace WarcArchiver.App.Controls;

/// <summary>A slim, flat progress bar that fits the dark theme (the native one ignores colours).</summary>
internal sealed class DarkProgressBar : Control
{
    int _value;
    int _maximum = 100;

    public DarkProgressBar()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
        Height = 8;
        TabStop = false;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set
        {
            int clamped = Math.Clamp(value, 0, _maximum);
            if (clamped != _value)
            {
                _value = clamped;
                Invalidate();
            }
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(1, value);
            _value = Math.Min(_value, _maximum);
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? DarkTheme.Background);

        var track = new RectangleF(0, 0, Width - 1, Height - 1);
        using (GraphicsPath trackPath = RoundedRectangle(track, track.Height / 2))
        using (var trackBrush = new SolidBrush(DarkTheme.SurfaceRaised))
        {
            e.Graphics.FillPath(trackBrush, trackPath);
        }

        float fraction = _maximum == 0 ? 0 : (float)_value / _maximum;
        float width = Math.Max(track.Height, track.Width * fraction);
        if (_value > 0)
        {
            var fill = new RectangleF(0, 0, width, track.Height);
            using GraphicsPath fillPath = RoundedRectangle(fill, fill.Height / 2);
            using var fillBrush = new SolidBrush(DarkTheme.Accent);
            e.Graphics.FillPath(fillBrush, fillPath);
        }
    }

    static GraphicsPath RoundedRectangle(RectangleF r, float radius)
    {
        float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
