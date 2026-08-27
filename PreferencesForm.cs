using System.Runtime.InteropServices;

namespace BackToTheFutureLauncher;

internal sealed class PreferencesForm : Form
{
    private sealed record ResolutionOption(int Width, int Height)
    {
        public override string ToString() => $"{Width} × {Height}";
    }

    private readonly IReadOnlyList<string> _paths;
    private readonly TelltalePreferences _preferences;
    private readonly ComboBox _displayMode = new();
    private readonly ComboBox _resolution = new();
    private readonly NumericUpDown _renderQuality = Number(1, 9);
    private readonly NumericUpDown _shadowQuality = Number(0, 2);
    private readonly NumericUpDown _antiAliasing = Number(0, 3);
    private readonly CheckBox _effects = new();
    private readonly CheckBox _subtitles = new();
    private readonly DarkSlider _musicVolume = new();
    private readonly DarkSlider _voiceVolume = new();
    private readonly DarkSlider _effectsVolume = new();

    public PreferencesForm(string sourceEpisode, IReadOnlyList<string> paths,
        TelltalePreferences preferences, Icon? icon)
    {
        _paths = paths;
        _preferences = preferences;
        Text = "Settings — All Episodes";
        ClientSize = new Size(510, 605);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(14, 18, 25);
        ForeColor = Color.FromArgb(226, 232, 240);
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ShowInTaskbar = false;
        if (icon is not null)
            Icon = icon;
        BuildInterface(sourceEpisode);
        PopulateValues();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        EnableDarkTitleBar();
    }

    private void BuildInterface(string sourceEpisode)
    {
        var title = new Label
        {
            Text = "GAME SETTINGS",
            Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(30, 21)
        };
        var subtitle = new Label
        {
            Text = $"Loaded from {sourceEpisode} • Saves to all {_paths.Count} episodes",
            ForeColor = Color.FromArgb(135, 146, 160),
            AutoSize = true,
            Location = new Point(33, 57)
        };

        var table = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 10,
            Location = new Point(30, 91),
            Size = new Size(450, 420),
            BackColor = Color.Transparent
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54F));
        for (int row = 0; row < table.RowCount; row++)
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, row < 7 ? 39F : 46F));

        _displayMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _displayMode.Items.AddRange(["On", "Off"]);
        _resolution.DropDownStyle = ComboBoxStyle.DropDownList;
        _resolution.Items.AddRange(new object[]
        {
            new ResolutionOption(800, 600),
            new ResolutionOption(1024, 768),
            new ResolutionOption(1280, 720),
            new ResolutionOption(1280, 800),
            new ResolutionOption(1366, 768),
            new ResolutionOption(1600, 900),
            new ResolutionOption(1680, 1050),
            new ResolutionOption(1920, 1080),
            new ResolutionOption(2560, 1080),
            new ResolutionOption(2560, 1440),
            new ResolutionOption(3440, 1440),
            new ResolutionOption(3840, 2160)
        });
        foreach (Control input in new Control[]
            { _displayMode, _resolution, _renderQuality, _shadowQuality, _antiAliasing })
        {
            input.BackColor = Color.FromArgb(31, 38, 48);
            input.ForeColor = Color.White;
            input.Margin = new Padding(0, 4, 0, 4);
            input.Width = 225;
        }
        _subtitles.Text = "Enabled";
        _subtitles.AutoSize = true;
        _subtitles.ForeColor = ForeColor;
        _effects.Text = "Enabled";
        _effects.AutoSize = true;
        _effects.ForeColor = ForeColor;
        _renderQuality.ValueChanged += (_, _) => UpdateAdvancedGraphicsAvailability();

        AddRow(table, 0, "Full screen", _displayMode);
        AddRow(table, 1, "Screen resolution", _resolution);
        AddRow(table, 2, "Graphics quality (1–9)", _renderQuality);
        AddRow(table, 3, "Shadow quality (0–2)", _shadowQuality);
        AddRow(table, 4, "Anti-aliasing (0–3)", _antiAliasing);
        AddRow(table, 5, "Effects", _effects);
        AddRow(table, 6, "Subtitles", _subtitles);
        AddRow(table, 7, "Music volume", _musicVolume);
        AddRow(table, 8, "Voice volume", _voiceVolume);
        AddRow(table, 9, "Effects volume", _effectsVolume);

        if (!_preferences.HasVoiceVolume)
        {
            _voiceVolume.Enabled = false;
            var voiceTip = new ToolTip();
            voiceTip.SetToolTip(_voiceVolume,
                "Voice is at its 100% default and is not stored yet. Change it once in this episode's game menu to enable editing.");
        }

        Button cancelButton = DialogButton("CANCEL", 264, Color.FromArgb(38, 45, 56), Color.White);
        cancelButton.DialogResult = DialogResult.Cancel;
        Button saveButton = DialogButton("SAVE ALL", 376, Color.FromArgb(19, 174, 235), Color.FromArgb(7, 16, 23));
        saveButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        saveButton.Click += (_, _) => SavePreferences();

        Controls.AddRange([title, subtitle, table, cancelButton, saveButton]);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void PopulateValues()
    {
        ResolutionOption? currentResolution = null;
        foreach (ResolutionOption option in _resolution.Items)
        {
            if (option.Width == _preferences.Width && option.Height == _preferences.Height)
            {
                currentResolution = option;
                break;
            }
        }
        if (currentResolution is null)
        {
            currentResolution = new ResolutionOption(_preferences.Width, _preferences.Height);
            _resolution.Items.Insert(0, currentResolution);
        }
        _resolution.SelectedItem = currentResolution;
        _displayMode.SelectedIndex = _preferences.Windowed ? 1 : 0;
        _renderQuality.Value = Math.Clamp(_preferences.RenderQuality, 1, 9);
        _shadowQuality.Value = Math.Clamp(_preferences.ShadowQuality, 0, 2);
        _antiAliasing.Value = Math.Clamp(_preferences.AntiAliasingQuality, 0, 3);
        _effects.Checked = _preferences.Effects;
        _subtitles.Checked = _preferences.Subtitles;
        _musicVolume.Value = Percent(_preferences.MusicVolume);
        _voiceVolume.Value = Percent(_preferences.VoiceVolume);
        _effectsVolume.Value = Percent(_preferences.EffectsVolume);
        UpdateAdvancedGraphicsAvailability();
    }

    private void SavePreferences()
    {
        if (_resolution.SelectedItem is not ResolutionOption resolution)
            return;

        _preferences.Width = resolution.Width;
        _preferences.Height = resolution.Height;
        _preferences.Windowed = _displayMode.SelectedIndex == 1;
        _preferences.RenderQuality = (int)_renderQuality.Value;
        _preferences.ShadowQuality = (int)_shadowQuality.Value;
        _preferences.AntiAliasingQuality = (int)_antiAliasing.Value;
        _preferences.Effects = _effects.Checked;
        _preferences.Subtitles = _subtitles.Checked;
        _preferences.MusicVolume = _musicVolume.Value / 100F;
        if (_preferences.HasVoiceVolume)
            _preferences.VoiceVolume = _voiceVolume.Value / 100F;
        _preferences.EffectsVolume = _effectsVolume.Value / 100F;

        try
        {
            foreach (string path in _paths)
                TelltalePreferences.Validate(path);
            foreach (string path in _paths)
                _preferences.Save(path);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Settings could not be saved to every episode. Restore any changed file from prefs.prop.bak if needed.\n\n{ex.Message}",
                "Settings error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static NumericUpDown Number(int minimum, int maximum) => new()
    {
        Minimum = minimum,
        Maximum = maximum,
        Width = 150,
        BorderStyle = BorderStyle.FixedSingle
    };

    private static int Percent(float value) =>
        Math.Clamp((int)MathF.Round(value * 100F), 0, 100);

    private void UpdateAdvancedGraphicsAvailability()
    {
        bool enabled = _renderQuality.Value > 6;
        _shadowQuality.Enabled = enabled;
        _effects.Enabled = enabled;
    }

    private static void AddRow(TableLayoutPanel table, int row, string text, Control input)
    {
        table.Controls.Add(new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = Color.FromArgb(190, 200, 212),
            Margin = new Padding(0, 8, 0, 0)
        }, 0, row);
        table.Controls.Add(input, 1, row);
    }

    private static Button DialogButton(string text, int x, Color background, Color foreground)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, 537),
            Size = new Size(104, 42),
            BackColor = background,
            ForeColor = foreground,
            FlatStyle = FlatStyle.Flat
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private void EnableDarkTitleBar()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            return;

        int enabled = 1;
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
