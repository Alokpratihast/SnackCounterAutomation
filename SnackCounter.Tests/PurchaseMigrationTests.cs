using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Tests;

public sealed class PurchaseMigrationTests
{
    [Fact]
    public async Task MigrateAsync_CreatesPurchaseQuantityAndUnitPriceColumns()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);

        await db.Database.MigrateAsync();

        var columns = new List<string>();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('Purchases');";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) columns.Add(reader.GetString(1));

        Assert.Contains("Quantity", columns);
        Assert.Contains("UnitPrice", columns);
    }
}
