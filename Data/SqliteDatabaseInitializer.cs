using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FnBManager.Data;

public static class SqliteDatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        // Existing SQLite databases created by EnsureCreated have no migration history.
        // Upgrade the legacy Orders table without rebuilding it or losing its data.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var connection = db.Database.GetDbConnection();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = "PRAGMA table_info(\"Orders\");";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(1));
            }
        }

        if (!columns.Contains("RequestHash"))
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Orders\" ADD COLUMN \"RequestHash\" TEXT NULL;");
        }

        if (!columns.Contains("RequestKey"))
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Orders\" ADD COLUMN \"RequestKey\" TEXT NULL;");
        }

        await db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orders_RequestKey\" ON \"Orders\" (\"RequestKey\") WHERE \"RequestKey\" IS NOT NULL;");
        await transaction.CommitAsync();
    }
}
