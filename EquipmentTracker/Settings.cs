using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace EquipmentTracker
{
    public class SettingsForm : Form
    {
        public AppSettings CurrentSettings { get; private set; }
        private ComboBox comboTheme, comboStartupTab;

        public SettingsForm(AppSettings settings)
        {
            this.CurrentSettings = settings.Clone(); // Work on a copy
            this.Text = "Settings";
            this.Size = new Size(400, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            this.Controls.Add(new Label { Text = "Theme:", Location = new Point(20, 20) });
            comboTheme = new ComboBox { Location = new Point(120, 20), DropDownStyle = ComboBoxStyle.DropDownList, Size = new Size(200, 20) };
            comboTheme.Items.AddRange(new[] { "Light", "Dark" });
            comboTheme.SelectedItem = CurrentSettings.ThemeName;
            this.Controls.Add(comboTheme);

            this.Controls.Add(new Label { Text = "Startup View:", Location = new Point(20, 60) });
            comboStartupTab = new ComboBox { Location = new Point(120, 60), DropDownStyle = ComboBoxStyle.DropDownList, Size = new Size(200, 20) };
            comboStartupTab.Items.AddRange(new[] { "Equipment", "History" });
            comboStartupTab.SelectedIndex = CurrentSettings.StartupTab;
            this.Controls.Add(comboStartupTab);

            var btnSave = new Button { Text = "Save", Location = new Point(120, 150), Size = new Size(100, 30), DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Cancel", Location = new Point(230, 150), Size = new Size(100, 30), DialogResult = DialogResult.Cancel };
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);

            btnSave.Click += (s, e) =>
            {
                CurrentSettings.ThemeName = comboTheme.SelectedItem.ToString();
                CurrentSettings.StartupTab = comboStartupTab.SelectedIndex;
            };
        }
    }

    public class AppSettings
    {
        private static readonly string _settingsFilePath = Path.Combine(GetAppDataPath(), "settings.json");
        public string ThemeName { get; set; } = "Light";
        public int StartupTab { get; set; } = 0; // 0 for Equipment, 1 for History

        [System.Text.Json.Serialization.JsonIgnore]
        public Theme Theme => Theme.GetTheme(ThemeName);

        public void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(_settingsFilePath, json);
        }

        public static AppSettings Load()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error loading settings: {ex.Message}", "ERROR");
                    return new AppSettings(); // Return default settings on error
                }
            }
            return new AppSettings();
        }

        public AppSettings Clone() => (AppSettings)this.MemberwiseClone();

        public static string GetAppDataPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "EquipmentTracker");
            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
            return appFolder;
        }
    }

    public class Theme
    {
        public string Name { get; set; }
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }
        public Color InputBackColor { get; set; }
        public Color InputForeColor { get; set; }
        public Color MenuBackColor { get; set; }
        public Color MenuForeColor { get; set; }
        public Color GridBackColor { get; set; }
        public Color GridCellBackColor { get; set; }
        public Color GridCellForeColor { get; set; }
        public Color GridHeaderBackColor { get; set; }
        public Color GridHeaderForeColor { get; set; }
        public Color EditColor { get; set; } // Color when editing a cell
        public Color WarningColor { get; set; }
        public Color WarningForeColor { get; set; }
        public bool UseAlternatingRows { get; set; }
        public Color GridAltCellBackColor { get; set; }

        public static Theme GetTheme(string themeName)
        {
            if (themeName == "Dark") return DarkTheme();
            return LightTheme();
        }

        public static Theme LightTheme()
        {
            return new Theme
            {
                Name = "Light",
                BackColor = SystemColors.Control,
                ForeColor = SystemColors.ControlText,
                InputBackColor = Color.White,
                InputForeColor = SystemColors.ControlText,
                MenuBackColor = SystemColors.MenuBar,
                MenuForeColor = SystemColors.MenuText,
                GridBackColor = Color.White,
                GridCellBackColor = Color.White,
                GridCellForeColor = SystemColors.ControlText,
                GridHeaderBackColor = SystemColors.ControlLight,
                GridHeaderForeColor = SystemColors.ControlText,
                EditColor = Color.LightYellow,
                WarningColor = Color.MistyRose,
                WarningForeColor = Color.DarkRed,
                UseAlternatingRows = true,
                GridAltCellBackColor = Color.AliceBlue
            };
        }

        public static Theme DarkTheme()
        {
            return new Theme
            {
                Name = "Dark",
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                InputBackColor = Color.FromArgb(63, 63, 70),
                InputForeColor = Color.White,
                MenuBackColor = Color.FromArgb(37, 37, 38),
                MenuForeColor = Color.White,
                GridBackColor = Color.FromArgb(37, 37, 38),
                GridCellBackColor = Color.FromArgb(51, 51, 55),
                GridCellForeColor = Color.White,
                GridHeaderBackColor = Color.FromArgb(63, 63, 70),
                GridHeaderForeColor = Color.White,
                EditColor = Color.FromArgb(70, 70, 0), // Darker yellow
                WarningColor = Color.FromArgb(80, 40, 40), // Darker red
                WarningForeColor = Color.LightCoral,
                UseAlternatingRows = true,
                GridAltCellBackColor = Color.FromArgb(60, 60, 65)
            };
        }
    }
}

