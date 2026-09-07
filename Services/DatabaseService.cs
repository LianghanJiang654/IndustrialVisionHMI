using Microsoft.Data.Sqlite;

namespace FactorialApp.Services;

public sealed class DatabaseService
{
    public string ConnectionString { get; }
    public DatabaseService(string path) => ConnectionString = $"Data Source={path}";
    public SqliteConnection Open() { var c = new SqliteConnection(ConnectionString); c.Open(); return c; }

    public void Initialize()
    {
        using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Inspections(
 Id INTEGER PRIMARY KEY AUTOINCREMENT, SerialNumber TEXT, Timestamp TEXT NOT NULL, ProductCode TEXT, RecipeName TEXT, RecipeVersion INTEGER,
 Passed INTEGER NOT NULL, NgCode TEXT, NgReason TEXT, X REAL, Y REAL, Angle REAL, Area REAL, ExposureUs REAL, Gain REAL,
 Operator TEXT, ImagePath TEXT, RawImagePath TEXT, CycleId INTEGER);
CREATE INDEX IF NOT EXISTS IX_Inspections_Time ON Inspections(Timestamp);
CREATE INDEX IF NOT EXISTS IX_Inspections_Product ON Inspections(ProductCode);
CREATE TABLE IF NOT EXISTS Alarms(Id INTEGER PRIMARY KEY AUTOINCREMENT, Timestamp TEXT NOT NULL, Code TEXT, Message TEXT, Source TEXT, IsAcknowledged INTEGER DEFAULT 0);
CREATE TABLE IF NOT EXISTS Recipes(Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Version INTEGER NOT NULL, CreatedAt TEXT NOT NULL, Json TEXT NOT NULL, UNIQUE(Name,Version));
CREATE TABLE IF NOT EXISTS Users(Id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT NOT NULL UNIQUE, PasswordHash TEXT NOT NULL, Salt TEXT NOT NULL, Role INTEGER NOT NULL);
";
        cmd.ExecuteNonQuery();
    }
}
