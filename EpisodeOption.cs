using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace BackToTheFutureLauncher;

internal sealed class EpisodeOption : Control
{
    private bool _checked;
    private bool _hovered;

    public Episode Episode { get; }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
                return;

            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? CheckedChanged;

    public EpisodeOption(Episode episode)
    {
        Episode = episode;
        Text = episode.Name;
        Cursor = Cursors.Hand;
        TabStop = true;
        Size = new Size(340, 46);
        Font = new Font("Segoe UI", 10.5F, FontStyle.Regular);
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.Selectable,
            true);
        BackColor = Color.Transparent;
    }

    protected override void OnClick(EventArgs e)
    {
        Checked = true;
        Focus();
        base.OnClick(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            Checked = true;
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        Rectangle cardBounds = new(1, 1, Width - 3, Height - 3);
        using GraphicsPath card = RoundedRectangle(cardBounds, 7);
        Color cardColor = Checked
            ? Color.FromArgb(50, 19, 174, 235)
            : _hovered
                ? Color.FromArgb(34, 255, 255, 255)
                : Color.FromArgb(12, 255, 255, 255);
        using var cardBrush = new SolidBrush(cardColor);
        graphics.FillPath(cardBrush, card);

        if (Focused)
        {
            using var focusPen = new Pen(Color.FromArgb(135, 19, 174, 235), 1F);
            graphics.DrawPath(focusPen, card);
        }

        RectangleF outerCircle = new(14F, (Height - 18F) / 2F, 18F, 18F);
        using var ringPen = new Pen(
            Checked ? Color.FromArgb(19, 174, 235) : Color.FromArgb(135, 146, 160),
            1.8F);
        graphics.DrawEllipse(ringPen, outerCircle);

        if (Checked)
        {
            RectangleF dot = new(19F, (Height - 8F) / 2F, 8F, 8F);
            using var dotBrush = new SolidBrush(Color.FromArgb(19, 174, 235));
            graphics.FillEllipse(dotBrush, dot);
        }

        Rectangle textBounds = new(43, 0, Width - 52, Height);
        Color textColor = Checked ? Color.White : Color.FromArgb(218, 224, 232);
        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            textBounds,
            textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
