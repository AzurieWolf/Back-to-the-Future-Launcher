using System.Drawing.Drawing2D;

namespace BackToTheFutureLauncher;

internal sealed class DarkSlider : Control
{
    private int _value;
    private bool _dragging;

    public int Value
    {
        get => _value;
        set
        {
            int clamped = Math.Clamp(value, 0, 100);
            if (_value == clamped)
                return;
            _value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? ValueChanged;

    public DarkSlider()
    {
        Size = new Size(225, 32);
        TabStop = true;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable |
            ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int trackWidth = Math.Max(20, Width - 55);
        var track = new Rectangle(2, Height / 2 - 3, trackWidth, 6);
        int filledWidth = (int)Math.Round(track.Width * Value / 100D);

        using var emptyBrush = new SolidBrush(Color.FromArgb(55, 66, 80));
        using var filledBrush = new SolidBrush(Color.FromArgb(19, 174, 235));
        e.Graphics.FillRectangle(emptyBrush, track);
        if (filledWidth > 0)
            e.Graphics.FillRectangle(filledBrush, track.X, track.Y, filledWidth, track.Height);

        int thumbX = track.X + filledWidth;
        using var thumbBrush = new SolidBrush(Focused ? Color.White : Color.FromArgb(205, 232, 242));
        e.Graphics.FillEllipse(thumbBrush, thumbX - 6, Height / 2 - 6, 12, 12);

        TextRenderer.DrawText(e.Graphics, $"{Value}%", Font,
            new Rectangle(track.Right + 10, 0, 43, Height),
            Color.FromArgb(180, 192, 205),
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragging = true;
            Focus();
            SetValueFromMouse(e.X);
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
            SetValueFromMouse(e.X);
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _dragging = false;
        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Left or Keys.Down)
            Value -= e.Control ? 10 : 5;
        else if (e.KeyCode is Keys.Right or Keys.Up)
            Value += e.Control ? 10 : 5;
        else
        {
            base.OnKeyDown(e);
            return;
        }
        e.Handled = true;
    }

    private void SetValueFromMouse(int x)
    {
        int trackWidth = Math.Max(20, Width - 55);
        Value = (int)Math.Round(Math.Clamp((x - 2D) / trackWidth, 0D, 1D) * 100D);
    }
}
