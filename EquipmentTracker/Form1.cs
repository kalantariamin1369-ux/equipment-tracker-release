using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EquipmentTracker
{
    public partial class Form1 : Form
    {
        // ... previous code omitted for brevity ...

        private async Task ImportFromCsvAsync()
        {
            var ofd = new OpenFileDialog { Filter = "CSV Files (*.csv)|*.csv", Title = "Import Equipment from CSV" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    SetLoadingState(true, "Importing from CSV...");
                    var lines = File.ReadAllLines(ofd.FileName).Skip(1).ToList();

                    var existingNames = new HashSet<string>(
                        _masterEquipmentList.Select(e => e.Name.ToLowerInvariant()),
                        StringComparer.OrdinalIgnoreCase);

                    var newEquipment = new List<Equipment>();
                    int skippedCount = 0;

                    foreach (var line in lines)
                    {
                        // FIX: use char overload for Split to avoid CS1503 (string -> char)
                        var fields = line.Split(',');
                        if (fields.Length < 2) continue;

                        string name = fields[0].Trim().Trim('"');
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
                        existingNames.Add(name.ToLowerInvariant());
                    }

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

        // ... remainder of file unchanged ...
    }
}
