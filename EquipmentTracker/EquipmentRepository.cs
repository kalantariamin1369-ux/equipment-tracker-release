using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EquipmentTracker
{
    public class EquipmentRepository
    {
        private readonly string _dbPath;

        public EquipmentRepository()
        {
            string appDataPath = AppSettings.GetAppDataPath();
            _dbPath = Path.Combine(appDataPath, "equipment.db");
            Logger.Log($"Database path: {_dbPath}");
        }

        private SQLiteConnection GetConnection() => new SQLiteConnection($"Data Source={_dbPath};Version=3;FailIfMissing=False");

        public async Task InitializeDatabaseAsync()
        {
            using (var cnn = GetConnection())
            {
                await cnn.OpenAsync();
                string createEquipmentTable = @"
                    CREATE TABLE IF NOT EXISTS Equipment (
                        Id TEXT PRIMARY KEY,
                        Name TEXT NOT NULL UNIQUE,
                        Quantity INTEGER NOT NULL,
                        Category TEXT,
                        MinStockLevel INTEGER NOT NULL DEFAULT 0,
                        LastUpdated DATETIME NOT NULL
                    );";
                string createTransactionTable = @"
                    CREATE TABLE IF NOT EXISTS Transactions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        EquipmentId TEXT NOT NULL,
                        Timestamp DATETIME NOT NULL,
                        ChangeType TEXT,
                        OldQuantity INTEGER NOT NULL,
                        NewQuantity INTEGER NOT NULL,
                        Notes TEXT,
                        FOREIGN KEY(EquipmentId) REFERENCES Equipment(Id) ON DELETE CASCADE
                    );";

                using (var cmd = new SQLiteCommand(cnn))
                {
                    cmd.CommandText = createEquipmentTable;
                    await cmd.ExecuteNonQueryAsync();
                    cmd.CommandText = createTransactionTable;
                    await cmd.ExecuteNonQueryAsync();
                }
                Logger.Log("Database initialized or already exists.");
            }
        }

        public async Task<List<Equipment>> GetAllAsync()
        {
            var list = new List<Equipment>();
            using (var cnn = GetConnection())
            {
                await cnn.OpenAsync();
                string sql = "SELECT Id, Name, Quantity, Category, MinStockLevel, LastUpdated FROM Equipment";
                using (var cmd = new SQLiteCommand(sql, cnn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new Equipment
                        {
                            Id = reader.GetString(0),
                            Name = reader.GetString(1),
                            Quantity = reader.GetInt32(2),
                            Category = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            MinStockLevel = reader.GetInt32(4),
                            LastUpdated = reader.GetDateTime(5)
                        });
                    }
                }
            }
            return list;
        }

        public async Task<Equipment> GetByIdAsync(string id)
        {
            using (var cnn = GetConnection())
            {
                await cnn.OpenAsync();
                var cmd = new SQLiteCommand("SELECT Id, Name, Quantity, Category, MinStockLevel, LastUpdated FROM Equipment WHERE Id = @id", cnn);
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Equipment
                        {
                            Id = reader.GetString(0),
                            Name = reader.GetString(1),
                            Quantity = reader.GetInt32(2),
                            Category = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            MinStockLevel = reader.GetInt32(4),
                            LastUpdated = reader.GetDateTime(5)
                        };
                    }
                }
            }
            return null;
        }

        public async Task<bool> DoesNameExistAsync(string name)
        {
            using (var cnn = GetConnection())
            {
                await cnn.OpenAsync();
                var cmd = new SQLiteCommand("SELECT 1 FROM Equipment WHERE Name = @name", cnn);
                cmd.Parameters.AddWithValue("@name", name);
                return (await cmd.ExecuteScalarAsync()) != null;
            }
        }

        public async Task AddAsync(Equipment eq, string changeType, string notes)
        {
            using (var cnn = GetConnection())
            {
                await cnn.OpenAsync();
                using (var transaction = cnn.BeginTransaction())
                {
                    try
                    {
                        var eqCmd = new SQLiteCommand("INSERT INTO Equipment (Id, Name, Quantity, Category, MinStockLevel, LastUpdated) VALUES (@id, @name, @qty, @cat, @min, @last)", cnn, transaction);
                        eqCmd.Parameters.AddWithValue("@id", eq.Id);
                        eqCmd.Parameters.AddWithValue("@name", eq.Name);
                        eqCmd.Parameters.AddWithValue("@qty", eq.Quantity);
                        eqCmd.Parameters.AddWithValue("@cat", eq.Category);
                        eqCmd.Parameters.AddWithValue("@min", eq.MinStockLevel);
                        eqCmd.Parameters.AddWithValue("@last", eq.LastUpdated);
                        await eqCmd.ExecuteNonQueryAsync();

                        var logCmd = new SQLiteCommand("INSERT INTO Transactions (EquipmentId, Timestamp, ChangeType, OldQuantity, NewQuantity, Notes) VALUES (@eqId, @ts, @type, @old, @new, @notes)", cnn, transaction);
                        logCmd.Parameters.AddWithValue("@eqId", eq.Id);
                        logCmd.Parameters.AddWithValue("@ts", eq.LastUpdated);
                        logCmd.Parameters.AddWithValue("@type", changeType);
                        logCmd.Parameters.AddWithValue("@old", 0);
                        logCmd.Parameters.AddWithValue("@new", eq.Quantity);
                        logCmd.Parameters.AddWithValue("@notes", notes);
                        await logCmd.ExecuteNonQueryAsync();

                        transaction.Commit();
                        Logger.Log($"Added equipment {eq.Name} and logged transaction.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Log($"Error adding equipment {eq.Name}: {ex.Message}", "ERROR");
                        throw;
                    }
                }
            }
        }

        public async Task AddBatchAsync(List<Equipment> equipmentList, string changeType, string notes)
        {
            using (var cnn = GetConnection())
            {
                await cnn.OpenAsync();
                using (var transaction = cnn.BeginTransaction())
                {
                    try
                    {
                        var eqCmd = new SQLiteCommand("INSERT INTO Equipment (Id, Name, Quantity, Category, MinStockLevel, LastUpdated) VALUES (@id, @name, @qty, @cat, @min, @last)", cnn, transaction);
                        var logCmd = new SQLiteCommand("INSERT INTO Transactions (EquipmentId, Timestamp, ChangeType, OldQuantity, NewQuantity, Notes) VALUES (@eqId, @ts, @type, @old, @new, @notes)", cnn, transaction);

                        foreach (var eq in equipmentList)
                        {
                            eqCmd.Parameters.Clear();
                            eqCmd.Parameters.AddWithValue("@id", eq.Id);
                            eqCmd.Parameters.AddWithValue("@name", eq.Name);
                            eqCmd.Parameters.AddWithValue("@qty", eq.Quantity);
                            eqCmd.Parameters.AddWithValue("@cat", eq.Category);
                            eqCmd.Parameters.AddWithValue("@min", eq.MinStockLevel);
                            eqCmd.Parameters.AddWithValue("@last", eq.LastUpdated);
                            await eqCmd.ExecuteNonQueryAsync();

                            logCmd.Parameters.Clear();
                            logCmd.Parameters.AddWithValue("@eqId", eq.Id);
                            logCmd.Parameters.AddWithValue("@ts", eq.LastUpdated);
                            logCmd.Parameters.AddWithValue("@type", changeType);
                            logCmd.Parameters.AddWithValue("@old", 0);
                            logCmd.Parameters.AddWithValue("@new", eq.Quantity);
                            logCmd.Parameters.AddWithValue("@notes", notes);
                            await logCmd.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                        Logger.Log($"Added {equipmentList.Count} equipment items in batch and logged transactions.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Log($"Error adding batch equipment: {ex.Message}", "ERROR");
                        throw;
                    }
                }
            }
        }

        public async Task UpdateAsync(Equipment eq, int oldQuantity, string changeType, string notes)
        {
            using (var cnn = GetConnection())
            {
                await cnn.OpenAsync();
                using (var transaction = cnn.BeginTransaction())
                {
                    try
                    {
                        var eqCmd = new SQLiteCommand("UPDATE Equipment SET Name = @name, Quantity = @qty, Category = @cat, MinStockLevel = @min, LastUpdated = @last WHERE Id = @id", cnn, transaction);
                        eqCmd.Parameters.AddWithValue("@id", eq.Id);
                        eqCmd.Parameters.AddWithValue("@name", eq.Name);
                        eqCmd.Parameters.AddWithValue("@qty", eq.Quantity);
                        eqCmd.Parameters.AddWithValue("@cat", eq.Category);
                        eqCmd.Parameters.AddWithValue("@min", eq.MinStockLevel);
                        eqCmd.Parameters.AddWithValue("@last", eq.LastUpdated);
                        await eqCmd.ExecuteNonQueryAsync();

                        var logCmd = new SQLiteCommand("INSERT INTO Transactions (EquipmentId, Timestamp, ChangeType, OldQuantity, NewQuantity, Notes) VALUES (@eqId, @ts, @type, @old, @new, @notes)", cnn, transaction);
                        logCmd.Parameters.AddWithValue("@eqId", eq.Id);
                        logCmd.Parameters.AddWithValue("@ts", eq.LastUpdated);
                        logCmd.Parameters.AddWithValue("@type", changeType);
                        logCmd.Parameters.AddWithValue("@old", oldQuantity);
                        logCmd.Parameters.AddWithValue("@new", eq.Quantity);
                        logCmd.Parameters.AddWithValue("@notes", notes);
                        await logCmd.ExecuteNonQueryAsync();

                        transaction.Commit();
                        Logger.Log($"Updated equipment {eq.Name} and logged transaction.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Log($"Error updating equipment {eq.Name}: {ex.Message}", "ERROR");
                        throw;
                    }
                }
            }
        }

        public async Task UpdateAsync(Equipment eq) // Simplified update for non-quantity changes (e.g., from grid edit)
        {
            using (var cnn = GetConnection())
            {
                await cnn.OpenAsync();
                var cmd = new SQLiteCommand("UPDATE Equipment SET Name = @name, Category = @cat, MinStockLevel = @min, LastUpdated = @last WHERE Id = @id", cnn);
                cmd.Parameters.AddWithValue("@id", eq.Id);
                cmd.Parameters.AddWithValue("@name", eq.Name);
                cmd.Parameters.AddWithValue("@cat", eq.Category);
                cmd.Parameters.AddWithValue("@min", eq.MinStockLevel);
                cmd.Parameters.AddWithValue("@last", eq.LastUpdated);
                await cmd.ExecuteNonQueryAsync();
                Logger.Log($"Updated equipment {eq.Name} (non-quantity changes).");
            }
        }

        public async Task DeleteAsync(string id)
        {
            using (var cnn = GetConnection())
            {
                await cnn.OpenAsync();
                // Due to ON DELETE CASCADE, related transactions will be deleted automatically.
                var cmd = new SQLiteCommand("DELETE FROM Equipment WHERE Id = @id", cnn);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                Logger.Log($"Deleted equipment with Id: {id}.");
            }
        }

        public async Task<List<Transaction>> GetHistoryAsync(string searchTerm, DateTime startDate, DateTime endDate)
        {
            var history = new List<Transaction>();
            using (var cnn = GetConnection())
            {
                await cnn.OpenAsync();
                var sql = new StringBuilder("SELECT T.Id, T.EquipmentId, E.Name, T.Timestamp, T.ChangeType, T.OldQuantity, T.NewQuantity, T.Notes FROM Transactions T JOIN Equipment E ON T.EquipmentId = E.Id WHERE T.Timestamp BETWEEN @start AND @end");
                var parameters = new List<SQLiteParameter>
                {
                    new SQLiteParameter("@start", startDate),
                    new SQLiteParameter("@end", endDate)
                };

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    sql.Append(" AND (E.Name LIKE @search OR T.Notes LIKE @search OR T.ChangeType LIKE @search)");
                    parameters.Add(new SQLiteParameter("@search", $"%{searchTerm}%"));
                }

                sql.Append(" ORDER BY T.Timestamp DESC");

                using (var cmd = new SQLiteCommand(sql.ToString(), cnn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            history.Add(new Transaction
                            {
                                Id = reader.GetInt32(0),
                                EquipmentId = reader.GetString(1),
                                EquipmentName = reader.GetString(2),
                                Timestamp = reader.GetDateTime(3),
                                ChangeType = reader.GetString(4),
                                OldQuantity = reader.GetInt32(5),
                                NewQuantity = reader.GetInt32(6),
                                Notes = reader.IsDBNull(7) ? "" : reader.GetString(7)
                            });
                        }
                    }
                }
            }
            return history;
        }
    }
}

