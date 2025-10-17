using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EquipmentTracker
{
    public partial class Form1 : Form
    {
        private readonly EquipmentRepository _repository;
        private AppSettings _settings;
        private List<Equipment> _masterEquipmentList;

        // UI Controls
        private MenuStrip mainMenuStrip;
        private TabControl mainTabControl;
        private DataGridView dgvEquipment, dgvHistory;
        private TextBox txtEquipmentName, txtCategory, txtSearch, txtHistorySearch;
        private NumericUpDown numQuantity, numBulkQty;
        private Button btnAdd, btnAddQty, btnRemoveQty, btnDelete;
        private ProgressBar progressBar;
        private Label statusLabel;
        private DateTimePicker dtpHistoryStart, dtpHistoryEnd;

        public Form1()
        {
            InitializeComponent();
            LoadSettings();
            _repository = new EquipmentRepository();
            InitializeUI();
            this.Load += async (s, e) => await LoadDataAsync();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ResumeLayout(false);
        }

        private void LoadSettings()
        {
            _settings = AppSettings.Load();
        }

        private void InitializeUI()
        {
            this.Text = "Equipment Tracker - Production Edition";
            this.Size = new Size(1000, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(950, 700);

            // --- Menu Strip ---
            mainMenuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add("Import from CSV...", null, async (s, e) => await ImportFromCsvAsync());
            fileMenu.DropDownItems.Add("Export to CSV...", null, ExportToCsv);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Backup Database...", null, BackupDatabase);
            fileMenu.DropDownItems.Add("E&xit", null, (s, e) => this.Close());

            var toolsMenu = new ToolStripMenuItem("&Tools");
            toolsMenu.DropDownItems.Add("&Settings...", null, OpenSettings);
            toolsMenu.DropDownItems.Add("View Logs...", null, ViewLogs);

            mainMenuStrip.Items.Add(fileMenu);
            mainMenuStrip.Items.Add(toolsMenu);
            this.Controls.Add(mainMenuStrip);
            this.MainMenuStrip = mainMenuStrip;

            // --- Main Tab Control ---
            mainTabControl = new TabControl { Dock = DockStyle.Fill, TabIndex = 0 };
            mainTabControl.Location = new Point(0, mainMenuStrip.Height);
            var equipmentTabPage = new TabPage("Equipment");
            var historyTabPage = new TabPage("Transaction History");
            mainTabControl.TabPages.Add(equipmentTabPage);
            mainTabControl.TabPages.Add(historyTabPage);
            this.Controls.Add(mainTabControl);

            // --- EQUIPMENT TAB ---
            var inputPanel = new GroupBox
            {
                Text = "Add / Modify Equipment",
                Location = new Point(15, 15),
                Size = new Size(950, 140),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            equipmentTabPage.Controls.Add(inputPanel);

            // Input Controls
            inputPanel.Controls.Add(new Label { Text = "Name:", Location = new Point(15, 30), Size = new Size(70, 20) });
            txtEquipmentName = new TextBox { Location = new Point(90, 30), Size = new Size(200, 20), TabIndex = 0 };
            inputPanel.Controls.Add(txtEquipmentName);

            inputPanel.Controls.Add(new Label { Text = "Initial Qty:", Location = new Point(15, 70), Size = new Size(70, 20) });
            numQuantity = new NumericUpDown { Location = new Point(90, 70), Size = new Size(80, 20), Minimum = 0, Maximum = 100000, TabIndex = 1 };
            inputPanel.Controls.Add(numQuantity);

            inputPanel.Controls.Add(new Label { Text = "Category:", Location = new Point(310, 30), Size = new Size(70, 20) });
            txtCategory = new TextBox { Location = new Point(380, 30), Size = new Size(150, 20), TabIndex = 2 };
            inputPanel.Controls.Add(txtCategory);

            btnAdd = new Button { Text = "Add New Item", Location = new Point(15, 100), Size = new Size(120, 30), TabIndex = 3 };
            btnAdd.Click += async (s, e) => await AddEquipmentAsync();
            inputPanel.Controls.Add(btnAdd);

            inputPanel.Controls.Add(new Label { Text = "Qty to Add/Remove:", Location = new Point(550, 30) });
            numBulkQty = new NumericUpDown { Location = new Point(670, 30), Size = new Size(80, 20), Minimum = 1, Maximum = 10000, Value = 1 };
            inputPanel.Controls.Add(numBulkQty);

            btnAddQty = new Button { Text = "Add", Location = new Point(760, 27), Size = new Size(85, 30), TabIndex = 4 };
            btnAddQty.Click += async (s, e) => await UpdateQuantity(true, (int)numBulkQty.Value);
            inputPanel.Controls.Add(btnAddQty);

            btnRemoveQty = new Button { Text = "Remove", Location = new Point(850, 27), Size = new Size(85, 30), TabIndex = 5 };
            btnRemoveQty.Click += async (s, e) => await UpdateQuantity(false, (int)numBulkQty.Value);
            inputPanel.Controls.Add(btnRemoveQty);

            progressBar = new ProgressBar { Location = new Point(550, 103), Size = new Size(385, 25), Style = ProgressBarStyle.Marquee, Visible = false };
            inputPanel.Controls.Add(progressBar);

            var gridPanel = new GroupBox
            {
                Text = "Equipment List (Double-click a cell to edit)",
                Location = new Point(15, 165),
                Size = new Size(950, 480),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            equipmentTabPage.Controls.Add(gridPanel);

            gridPanel.Controls.Add(new Label { Text = "Search:", Location = new Point(15, 30), Size = new Size(60, 20) });
            txtSearch = new TextBox { Location = new Point(80, 30), Size = new Size(250, 20), TabIndex = 6 };
            txtSearch.TextChanged += (s, e) => ApplyFilterAndSort();
            gridPanel.Controls.Add(txtSearch);

            btnDelete = new Button { Text = "Delete Selected", Location = new Point(810, 27), Size = new Size(120, 30), TabIndex = 9, BackColor = Color.LightCoral };
            btnDelete.Click += async (s, e) => await DeleteEquipmentAsync();
            gridPanel.Controls.Add(btnDelete);

            dgvEquipment = new DataGridView
            {
                Location = new Point(15, 70),
                Size = new Size(920, 390),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                TabIndex = 10,
                ReadOnly = false,
                RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing,
                AllowUserToResizeRows = false,
                CellBorderStyle = DataGridViewCellBorderStyle.Single
            };
            dgvEquipment.DoubleBuffered(true);
            gridPanel.Controls.Add(dgvEquipment);

            dgvEquipment.CellBeginEdit += (s, e) => dgvEquipment.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = _settings.Theme.EditColor;
            dgvEquipment.CellEndEdit += async (s, e) => await DgvEquipment_CellEndEdit(s, e);
            dgvEquipment.CellFormatting += DgvEquipment_CellFormatting;
            dgvEquipment.ColumnHeaderMouseClick += (s, e) => ApplyFilterAndSort();

            // --- HISTORY TAB ---
            var historyFilterPanel = new GroupBox
            {
                Text = "Filter History",
                Location = new Point(15, 15),
                Size = new Size(950, 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            historyTabPage.Controls.Add(historyFilterPanel);

            historyFilterPanel.Controls.Add(new Label { Text = "Search:", Location = new Point(15, 30) });
            txtHistorySearch = new TextBox { Location = new Point(70, 30), Size = new Size(200, 20) };
            historyFilterPanel.Controls.Add(txtHistorySearch);

            historyFilterPanel.Controls.Add(new Label { Text = "From:", Location = new Point(290, 30) });
            dtpHistoryStart = new DateTimePicker { Location = new Point(335, 30), Format = DateTimePickerFormat.Short, Size = new Size(100, 20), Value = DateTime.Now.AddMonths(-1) };
            historyFilterPanel.Controls.Add(dtpHistoryStart);

            historyFilterPanel.Controls.Add(new Label { Text = "To:", Location = new Point(450, 30) });
            dtpHistoryEnd = new DateTimePicker { Location = new Point(480, 30), Format = DateTimePickerFormat.Short, Size = new Size(100, 20), Value = DateTime.Now };
            historyFilterPanel.Controls.Add(dtpHistoryEnd);

            var btnFilterHistory = new Button { Text = "Apply Filter", Location = new Point(600, 27), Size = new Size(100, 30) };
            btnFilterHistory.Click += async (s, e) => await LoadHistoryAsync();
            historyFilterPanel.Controls.Add(btnFilterHistory);

            dgvHistory = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                Location = new Point(15, 95),
                Size = new Size(950, 550),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvHistory.DoubleBuffered(true);
            historyTabPage.Controls.Add(dgvHistory);

            mainTabControl.SelectedIndexChanged += async (s, e) =>
            {
                if (mainTabControl.SelectedTab == historyTabPage)
                    await LoadHistoryAsync();
            };

            // --- Status Bar ---
            statusLabel = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Bottom,
                Height = 20,
                Padding = new Padding(5, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                BorderStyle = BorderStyle.Fixed3D
            };
            this.Controls.Add(statusLabel);

            // Apply theme and startup settings
            ApplyTheme();
            mainTabControl.SelectTab(_settings.StartupTab);
        }

        #region Settings & Theme
        private void OpenSettings(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm(_settings))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    _settings = settingsForm.CurrentSettings;
                    _settings.Save();
                    ApplyTheme();
                    MessageBox.Show("Settings saved. Some changes may require a restart to take full effect.",
                        "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ApplyTheme()
        {
            var theme = _settings.Theme;
            this.BackColor = theme.BackColor;
            this.ForeColor = theme.ForeColor;

            foreach (Control c in this.Controls)
            {
                UpdateControlTheme(c, theme);
            }

            // DataGridView special handling
            dgvEquipment.BackgroundColor = theme.GridBackColor;
            dgvEquipment.DefaultCellStyle.BackColor = theme.GridCellBackColor;
            dgvEquipment.DefaultCellStyle.ForeColor = theme.GridCellForeColor;
            dgvEquipment.ColumnHeadersDefaultCellStyle.BackColor = theme.GridHeaderBackColor;
            dgvEquipment.ColumnHeadersDefaultCellStyle.ForeColor = theme.GridHeaderForeColor;
            dgvEquipment.EnableHeadersVisualStyles = false;

            dgvHistory.BackgroundColor = theme.GridBackColor;
            dgvHistory.DefaultCellStyle.BackColor = theme.GridCellBackColor;
            dgvHistory.DefaultCellStyle.ForeColor = theme.GridCellForeColor;
            dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = theme.GridHeaderBackColor;
            dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = theme.GridHeaderForeColor;
            dgvHistory.EnableHeadersVisualStyles = false;

            dgvEquipment.Refresh();
            dgvHistory.Refresh();
        }

        private void UpdateControlTheme(Control control, Theme theme)
        {
            control.BackColor = theme.BackColor;
            control.ForeColor = theme.ForeColor;

            if (control is Button || control is TextBox || control is NumericUpDown ||
                control is DateTimePicker || control is ComboBox)
            {
                control.BackColor = theme.InputBackColor;
                control.ForeColor = theme.InputForeColor;
            }
            if (control is MenuStrip)
            {
                control.BackColor = theme.MenuBackColor;
                control.ForeColor = theme.MenuForeColor;
            }

            foreach (Control c in control.Controls)
            {
                UpdateControlTheme(c, theme);
            }
        }
        #endregion

        #region Core Data Operations
        private async Task LoadDataAsync()
        {
            try
            {
                SetLoadingState(true, "Loading data from database...");
                await _repository.InitializeDatabaseAsync();
                _masterEquipmentList = await _repository.GetAllAsync();
                var sortableList = new SortableBindingList<Equipment>(_masterEquipmentList);
                dgvEquipment.DataSource = sortableList;

                SetupGridColumns();
                ApplyFilterAndSort();
                SetStatus($"Loaded {_masterEquipmentList.Count} equipment items");
                Logger.Log($"Loaded {_masterEquipmentList.Count} equipment items");
            }
            catch (Exception ex)
            {
                Logger.Log($"Fatal error loading database: {ex.Message}", "ERROR");
                MessageBox.Show($"Fatal error loading database: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Error loading database");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async Task LoadHistoryAsync()
        {
            try
            {
                SetLoadingState(true, "Loading transaction history...");
                var history = await _repository.GetHistoryAsync(txtHistorySearch.Text,
                    dtpHistoryStart.Value.Date, dtpHistoryEnd.Value.Date.AddDays(1).AddTicks(-1));
                dgvHistory.DataSource = history;

                if (dgvHistory.Columns.Contains("EquipmentName"))
                    dgvHistory.Columns["EquipmentName"].HeaderText = "Equipment Name";
                if (dgvHistory.Columns.Contains("EquipmentId"))
                    dgvHistory.Columns["EquipmentId"].Visible = false;
                if (dgvHistory.Columns.Contains("Timestamp"))
                    dgvHistory.Columns["Timestamp"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";

                SetStatus($"Loaded {history.Count} history records.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading history: {ex.Message}", "ERROR");
                MessageBox.Show($"Error loading history: {ex.Message}", "History Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async Task AddEquipmentAsync()
        {
            string name = txtEquipmentName.Text.Trim();

            // Enhanced validation
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Equipment name cannot be empty.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEquipmentName.Focus();
                return;
            }

            if (name.Length > 100)
            {
                MessageBox.Show("Equipment name too long (max 100 characters).", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEquipmentName.Focus();
                return;
            }

            if (name.Any(c => c == '\'' || c == '"' || c == ';' || c == '\\'))
            {
                MessageBox.Show("Equipment name contains invalid characters.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEquipmentName.Focus();
                return;
            }

            if (await _repository.DoesNameExistAsync(name))
            {
                MessageBox.Show("Equipment with this name already exists.", "Duplicate Entry",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SetLoadingState(true, "Adding equipment...");
                var newEquipment = new Equipment
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Quantity = (int)numQuantity.Value,
                    Category = txtCategory.Text.Trim(),
                    MinStockLevel = 0,
                    LastUpdated = DateTime.Now
                };

                await _repository.AddAsync(newEquipment, "Initial Creation", "Added via UI.");
                _masterEquipmentList.Add(newEquipment);
                ApplyFilterAndSort();
                ClearInputs();
                SetStatus($"Added: {newEquipment.Name}");
                Logger.Log($"Added equipment: {newEquipment.Name}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding equipment: {ex.Message}", "ERROR");
                MessageBox.Show($"Error adding equipment: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async Task UpdateQuantity(bool isAdding, int quantity)
        {
            var selected = GetSelectedEquipment();
            if (selected == null)
            {
                MessageBox.Show("Please select an equipment from the list first.", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int quantityChange = isAdding ? quantity : -quantity;
            string note = $"{(isAdding ? "Added" : "Removed")} {quantity} via UI controls.";
            await UpdateItemQuantity(selected, quantityChange, "Manual Update", note);
        }

        private async Task UpdateItemQuantity(Equipment item, int quantityChange, string changeType, string notes)
        {
            if (quantityChange == 0) return;

            // Re-fetch current value to prevent race conditions
            var currentItem = await _repository.GetByIdAsync(item.Id);
            if (currentItem == null)
            {
                MessageBox.Show("Item was deleted by another process.", "Concurrency Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                await LoadDataAsync(); // Refresh data
                return;
            }

            if (quantityChange < 0 && currentItem.Quantity < Math.Abs(quantityChange))
            {
                MessageBox.Show($"Cannot remove {Math.Abs(quantityChange)}. Only {currentItem.Quantity} available.",
                    "Insufficient Quantity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await LoadDataAsync(); // Refresh data
                return;
            }

            try
            {
                SetLoadingState(true, "Updating quantity...");
                int oldQty = currentItem.Quantity;
                currentItem.Quantity += quantityChange;
                currentItem.LastUpdated = DateTime.Now;

                await _repository.UpdateAsync(currentItem, oldQty, changeType, notes);

                // Update local list
                var localItem = _masterEquipmentList.FirstOrDefault(e => e.Id == item.Id);
                if (localItem != null)
                {
                    localItem.Quantity = currentItem.Quantity;
                    localItem.LastUpdated = currentItem.LastUpdated;
                }

                dgvEquipment.Refresh();
                ClearInputs();
                SetStatus($"Updated quantity for {currentItem.Name}");
                Logger.Log($"Updated {currentItem.Name}: {oldQty} -> {currentItem.Quantity}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error updating quantity: {ex.Message}", "ERROR");
                MessageBox.Show($"Error updating quantity: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async Task DeleteEquipmentAsync()
        {
            var selected = GetSelectedEquipment();
            if (selected == null) return;

            var result = MessageBox.Show(
                $"This will permanently delete '{selected.Name}' and all its history. This cannot be undone. Are you sure?",
                "Confirm Permanent Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    SetLoadingState(true, "Deleting equipment...");
                    await _repository.DeleteAsync(selected.Id);
                    _masterEquipmentList.Remove(selected);
                    ApplyFilterAndSort();
                    SetStatus($"Deleted: {selected.Name}");
                    Logger.Log($"Deleted equipment: {selected.Name}");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error deleting equipment: {ex.Message}", "ERROR");
                    MessageBox.Show($"Error deleting equipment: {ex.Message}", "Database Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    SetLoadingState(false);
                }
            }
        }
        #endregion

        #region CSV Import/Export
        private async Task ImportFromCsvAsync()
        {
            var ofd = new OpenFileDialog { Filter = "CSV Files (*.csv)|*.csv", Title = "Import Equipment from CSV" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    SetLoadingState(true, "Importing from CSV...");
                    var lines = File.ReadAllLines(ofd.FileName).Skip(1).ToList();

                    // Load existing names ONCE for efficient checking
                    var existingNames = new HashSet<string>(
                        _masterEquipmentList.Select(e => e.Name.ToLowerInvariant()),
                        StringComparer.OrdinalIgnoreCase);

                    var newEquipment = new List<Equipment>();
                    int skippedCount = 0;

                    foreach (var line in lines)
                    {
                        var fields = line.Split(",");
                        if (fields.Length < 2) continue; // Skip malformed lines

                        string name = fields[0].Trim().Trim('"'); // Remove quotes
                        if (existingNames.Contains(name.ToLowerInvariant()))
                        {
                            skippedCount++;
                            continue;
                        }

                        var equipment = new Equipment
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = name,
                            Quantity = int.TryParse(fields[1].Trim('"'), out int qty) ? qty : 0,
                            Category = fields.Length > 2 ? fields[2].Trim().Trim('"') : "",
                            MinStockLevel = fields.Length > 3 && int.TryParse(fields[3].Trim('"'), out int min) ? min : 0,
                            LastUpdated = DateTime.Now
                        };

                        newEquipment.Add(equipment);
                        existingNames.Add(name.ToLowerInvariant()); // Add to set to prevent duplicates within the same import
                    }

                    // Batch insert with transaction for performance and data integrity
                    if (newEquipment.Count > 0)
                    {
                        await _repository.AddBatchAsync(newEquipment, "CSV Import",
                            $"Imported from {Path.GetFileName(ofd.FileName)}");
                        _masterEquipmentList.AddRange(newEquipment);
                    }

                    ApplyFilterAndSort();
                    SetStatus($"Import complete. Added: {newEquipment.Count}, Skipped: {skippedCount}");
                    Logger.Log($"CSV Import: Added {newEquipment.Count}, Skipped {skippedCount}");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error importing CSV: {ex.Message}", "ERROR");
                    MessageBox.Show($"Error importing from CSV: {ex.Message}", "Import Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    SetLoadingState(false);
                }
            }
        }

        private void ExportToCsv(object sender, EventArgs e)
        {
            var grid = mainTabControl.SelectedTab.Controls.OfType<DataGridView>().FirstOrDefault();
            if (grid == null || grid.Rows.Count == 0)
            {
                MessageBox.Show("There is no data to export in the current view.", "No Data",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"{mainTabControl.SelectedTab.Text}_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    SetLoadingState(true, "Exporting...");
                    var sb = new StringBuilder();

                    // Headers
                    var headers = grid.Columns.Cast<DataGridViewColumn>()
                        .Where(c => c.Visible)
                        .Select(c => EscapeCsvField(c.HeaderText));
                    sb.AppendLine(string.Join(",", headers));

                    // Rows
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        var cells = row.Cells.Cast<DataGridViewCell>()
                            .Where(c => c.OwningColumn.Visible)
                            .Select(c => EscapeCsvField(c.Value?.ToString() ?? ""));
                        sb.AppendLine(string.Join(",", cells));
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    SetStatus("Export complete.");
                    Logger.Log($"Exported to CSV: {sfd.FileName}");
                    MessageBox.Show("Export completed successfully.", "Export Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Export error: {ex.Message}", "ERROR");
                    MessageBox.Show($"Export Error: {ex.Message}", "Export Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    SetLoadingState(false);
                }
            }
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            bool needsQuotes = field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r");
            if (needsQuotes)
            {
                var escaped = field.Replace("\"", "\"\"");
                return "\"" + escaped + "\"";
            }
            return field;
        }
        #endregion

        #region Backup & Logs
        private void BackupDatabase(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "Database Backup (*.db)|*.db",
                FileName = $"equipment_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string dbPath = Path.Combine(AppSettings.GetAppDataPath(), "equipment.db");
                    if (File.Exists(dbPath))
                    {
                        File.Copy(dbPath, sfd.FileName, true);
                        Logger.Log($"Database backup created: {sfd.FileName}");
                        MessageBox.Show("Backup created successfully.", "Backup Complete",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("No database file found to backup.", "Backup Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Backup failed: {ex.Message}", "ERROR");
                    MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ViewLogs(object sender, EventArgs e)
        {
            try
            {
                string logPath = Path.Combine(AppSettings.GetAppDataPath(), "logs");
                if (Directory.Exists(logPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", logPath);
                }
                else
                {
                    MessageBox.Show("No logs directory found.", "Logs", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Grid Management
        private async Task DgvEquipment_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var grid = (DataGridView)sender;
            grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = _settings.Theme.GridCellBackColor;
            var equipment = grid.Rows[e.RowIndex].DataBoundItem as Equipment;

            if (equipment != null)
            {
                try
                {
                    SetLoadingState(true, "Saving changes...");
                    equipment.LastUpdated = DateTime.Now;
                    await _repository.UpdateAsync(equipment); // This simplified update doesn't log quantity changes
                    grid.Refresh();
                    SetStatus($"Updated {equipment.Name}.");
                    Logger.Log($"Updated equipment via grid: {equipment.Name}");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to save grid changes: {ex.Message}", "ERROR");
                    MessageBox.Show($"Failed to save changes: {ex.Message}", "Update Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    SetLoadingState(false);
                }
            }
        }

        private void DgvEquipment_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = (DataGridView)sender;
            var row = grid.Rows[e.RowIndex];
            var equipment = row.DataBoundItem as Equipment;

            // Apply alternating row color
            if (_settings.Theme.UseAlternatingRows && e.RowIndex % 2 == 1)
            {
                row.DefaultCellStyle.BackColor = _settings.Theme.GridAltCellBackColor;
            }
            else
            {
                row.DefaultCellStyle.BackColor = _settings.Theme.GridCellBackColor;
            }

            // Low stock warning overrides other colors
            if (equipment != null && equipment.Quantity <= equipment.MinStockLevel && equipment.MinStockLevel > 0)
            {
                row.DefaultCellStyle.BackColor = _settings.Theme.WarningColor;
                row.DefaultCellStyle.ForeColor = _settings.Theme.WarningForeColor;
            }
            else
            {
                row.DefaultCellStyle.ForeColor = _settings.Theme.GridCellForeColor;
            }
        }

        private void SetupGridColumns()
        {
            if (dgvEquipment.Columns.Contains("Id"))
                dgvEquipment.Columns["Id"].Visible = false;

            if (dgvEquipment.Columns.Contains("Name"))
            {
                dgvEquipment.Columns["Name"].DisplayIndex = 0;
                dgvEquipment.Columns["Name"].Width = 200;
            }
            if (dgvEquipment.Columns.Contains("Category"))
            {
                dgvEquipment.Columns["Category"].DisplayIndex = 1;
                dgvEquipment.Columns["Category"].Width = 150;
            }
            if (dgvEquipment.Columns.Contains("Quantity"))
            {
                dgvEquipment.Columns["Quantity"].DisplayIndex = 2;
                dgvEquipment.Columns["Quantity"].Width = 100;
            }
            if (dgvEquipment.Columns.Contains("MinStockLevel"))
            {
                dgvEquipment.Columns["MinStockLevel"].DisplayIndex = 3;
                dgvEquipment.Columns["MinStockLevel"].HeaderText = "Min Stock";
                dgvEquipment.Columns["MinStockLevel"].Width = 100;
            }
            if (dgvEquipment.Columns.Contains("LastUpdated"))
            {
                dgvEquipment.Columns["LastUpdated"].DisplayIndex = 4;
                dgvEquipment.Columns["LastUpdated"].HeaderText = "Last Updated";
                dgvEquipment.Columns["LastUpdated"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
                dgvEquipment.Columns["LastUpdated"].ReadOnly = true;
                dgvEquipment.Columns["LastUpdated"].Width = 150;
            }

            // Ensure remaining columns fill space, or set specific widths
            dgvEquipment.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void ApplyFilterAndSort()
        {
            var filterText = txtSearch.Text.Trim();
            var sourceList = (dgvEquipment.DataSource as SortableBindingList<Equipment>);
            if (sourceList == null) return;

            var filtered = _masterEquipmentList;
            if (!string.IsNullOrWhiteSpace(filterText))
            {
                filtered = _masterEquipmentList.Where(e =>
                    e.Name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (e.Category != null && e.Category.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            }

            sourceList.Reset(filtered);
            SetStatus($"{sourceList.Count} items found.");
        }

        private Equipment GetSelectedEquipment() => dgvEquipment.SelectedRows.Count > 0 ? dgvEquipment.SelectedRows[0].DataBoundItem as Equipment : null;

        private void ClearInputs()
        {
            txtEquipmentName.Clear();
            txtCategory.Clear();
            numQuantity.Value = 0;
            txtEquipmentName.Focus();
        }
        #endregion

        #region UI State Management
        private void SetLoadingState(bool isLoading, string message = "")
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetLoadingState(isLoading, message)));
                return;
            }
            progressBar.Visible = isLoading;
            this.UseWaitCursor = isLoading;
            mainMenuStrip.Enabled = !isLoading;
            mainTabControl.Enabled = !isLoading;
            if (!string.IsNullOrEmpty(message)) SetStatus(message);
        }

        private void SetStatus(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetStatus(message)));
                return;
            }
            statusLabel.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }
        #endregion
    }
}
