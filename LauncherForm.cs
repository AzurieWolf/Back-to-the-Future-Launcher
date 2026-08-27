using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace BackToTheFutureLauncher;

internal sealed class LauncherForm : Form
{
    private readonly LauncherConfig _config;
    private readonly string _baseDirectory;
    private readonly List<EpisodeOption> _episodeOptions = [];
    private readonly Button _playButton = new();
    private readonly Label _statusLabel = new();
    private Image? _backgroundImage;
    private Icon? _windowIcon;

    public LauncherForm(LauncherConfig config, string baseDirectory)
    {
        _config = config;
        _baseDirectory = baseDirectory;

        Text = config.Title;
        ClientSize = new Size(config.Width, config.Height);
        MinimumSize = Size;
        MaximumSize = Size;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowIcon = true;
        ShowInTaskbar = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(12, 15, 20);
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 10F);
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        LoadWindowIcon();
        LoadBackground();
        BuildInterface();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        EnableDarkTitleBar();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        if (_backgroundImage is not null)
            DrawImageCover(e.Graphics, _backgroundImage, ClientRectangle);
        else
        {
            using var fallback = new LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(20, 27, 39),
                Color.FromArgb(5, 7, 11),
                35F);
            e.Graphics.FillRectangle(fallback, ClientRectangle);
        }

        using var shade = new LinearGradientBrush(
            ClientRectangle,
            Color.FromArgb(35, 0, 0, 0),
            Color.FromArgb(180, 0, 0, 0),
            LinearGradientMode.Horizontal);
        e.Graphics.FillRectangle(shade, ClientRectangle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _backgroundImage?.Dispose();
            _windowIcon?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void BuildInterface()
    {
        var content = new Panel
        {
            BackColor = Color.FromArgb(220, 14, 18, 25),
            Location = new Point(48, 48),
            Size = new Size(Math.Min(440, ClientSize.Width - 96), ClientSize.Height - 96),
            Padding = new Padding(34, 30, 34, 26)
        };

        var accent = new Panel
        {
            BackColor = Color.FromArgb(19, 174, 235),
            Dock = DockStyle.Left,
            Width = 4
        };

        var heading = new Label
        {
            AutoSize = false,
            Text = _config.Heading,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            Location = new Point(34, 28),
            Size = new Size(content.Width - 68, 42),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var subtitle = new Label
        {
            AutoSize = false,
            Text = "Choose an episode to begin",
            ForeColor = Color.FromArgb(165, 175, 188),
            Font = new Font("Segoe UI", 10F),
            Location = new Point(37, 72),
            Size = new Size(content.Width - 74, 24)
        };

        var episodeList = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Location = new Point(31, 112),
            Size = new Size(content.Width - 58, content.Height - 232),
            Padding = new Padding(0, 2, 0, 2)
        };

        foreach (Episode episode in _config.Episodes)
        {
            var option = new EpisodeOption(episode)
            {
                Size = new Size(episodeList.ClientSize.Width - 22, 46),
                Margin = new Padding(4, 1, 4, 3)
            };
            option.CheckedChanged += (_, _) =>
            {
                if (option.Checked)
                    SelectEpisode(option);
            };
            _episodeOptions.Add(option);
            episodeList.Controls.Add(option);
        }

        _playButton.Text = "PLAY EPISODE";
        _playButton.Enabled = false;
        _playButton.BackColor = Color.FromArgb(19, 174, 235);
        _playButton.ForeColor = Color.FromArgb(7, 16, 23);
        _playButton.FlatStyle = FlatStyle.Flat;
        _playButton.FlatAppearance.BorderSize = 0;
        _playButton.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
        _playButton.Cursor = Cursors.Hand;
        _playButton.Location = new Point(35, content.Height - 101);
        _playButton.Size = new Size(content.Width - 70, 46);
        _playButton.Click += (_, _) => LaunchSelectedEpisode();

        _statusLabel.AutoSize = false;
        _statusLabel.Text = "Select an episode";
        _statusLabel.ForeColor = Color.FromArgb(135, 146, 160);
        _statusLabel.Font = new Font("Segoe UI", 8.5F);
        _statusLabel.Location = new Point(35, content.Height - 47);
        _statusLabel.Size = new Size(content.Width - 70, 22);
        _statusLabel.TextAlign = ContentAlignment.MiddleCenter;

        content.Controls.Add(accent);
        content.Controls.Add(heading);
        content.Controls.Add(subtitle);
        content.Controls.Add(episodeList);
        content.Controls.Add(_playButton);
        content.Controls.Add(_statusLabel);
        Controls.Add(content);

        AcceptButton = _playButton;

        if (_episodeOptions.Count > 0)
            _episodeOptions[0].Checked = true;
    }

    private void LoadBackground()
    {
        string path = ResolveFromLauncher(_config.Background);
        if (!File.Exists(path))
            return;

        // Clone the image so the source file is not locked while the launcher is open.
        using Image source = Image.FromFile(path);
        _backgroundImage = new Bitmap(source);
    }

    private void LoadWindowIcon()
    {
        string iconPath = ResolveFromLauncher(_config.Icon);

        try
        {
            _windowIcon = File.Exists(iconPath)
                ? new Icon(iconPath)
                : Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? Application.ExecutablePath);

            if (_windowIcon is not null)
                Icon = _windowIcon;
        }
        catch
        {
            // A missing or malformed companion icon must not prevent the launcher opening.
        }
    }

    private void LaunchSelectedEpisode()
    {
        Episode? episode = _episodeOptions
            .FirstOrDefault(option => option.Checked)?.Episode;
        if (episode is null)
            return;

        string executablePath = ResolveFromLauncher(episode.Executable);
        if (!File.Exists(executablePath))
        {
            _statusLabel.Text = "Executable not found";
            MessageBox.Show(
                $"Could not find the episode executable:\n\n{executablePath}\n\nCheck the executable value in launcher.ini.",
                "Episode not found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? _baseDirectory,
                UseShellExecute = true
            });
            Close();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Launch failed";
            MessageBox.Show(
                $"The episode could not be started.\n\n{ex.Message}",
                "Launch failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void SelectEpisode(EpisodeOption selected)
    {
        foreach (EpisodeOption option in _episodeOptions)
        {
            if (!ReferenceEquals(option, selected))
                option.Checked = false;
        }

        _playButton.Enabled = true;
        _statusLabel.Text = "Ready to launch";
    }

    private string ResolveFromLauncher(string configuredPath)
    {
        string normalized = configuredPath.Trim().Trim('"')
            .Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.IsPathRooted(normalized)
            ? normalized
            : Path.Combine(_baseDirectory, normalized));
    }

    private static void DrawImageCover(Graphics graphics, Image image, Rectangle bounds)
    {
        float scale = Math.Max(
            (float)bounds.Width / image.Width,
            (float)bounds.Height / image.Height);
        int width = (int)Math.Ceiling(image.Width * scale);
        int height = (int)Math.Ceiling(image.Height * scale);
        int x = bounds.X + (bounds.Width - width) / 2;
        int y = bounds.Y + (bounds.Height - height) / 2;
        graphics.DrawImage(image, new Rectangle(x, y, width, height));
    }

    private void EnableDarkTitleBar()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            return;

        int enabled = 1;
        // Attribute 20 is current; older Windows 10 builds use attribute 19.
        if (DwmSetWindowAttribute(Handle, 20, ref enabled, sizeof(int)) != 0)
            DwmSetWindowAttribute(Handle, 19, ref enabled, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);
}
