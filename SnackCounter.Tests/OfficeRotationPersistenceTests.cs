
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Domain.Entities;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Tests;

public class OfficeRotationPersistenceTests
{
    [Fact]
    public async Task RotationCursor_ShouldPersist_AfterDbContextRecreation()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var oslo = new Office("Oslo", "NOK");
        var bengaluru = new Office("Bengaluru", "INR");

        var amit = new Employee(
            "Amit", "amit@test.com", oslo.Id, true);

        var rahul = new Employee(
            "Rahul", "rahul@test.com", bengaluru.Id, true);

        await using (var db = new AppDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();

            db.Offices.AddRange(oslo, bengaluru);
            db.Employees.AddRange(amit, rahul);

            await db.SaveChangesAsync();

            oslo.UpdateVolunteerRotation(amit.Id);
            bengaluru.UpdateVolunteerRotation(rahul.Id);

            await db.SaveChangesAsync();
        }

        // Recreate DbContext using the same database connection.
        await using (var db = new AppDbContext(options))
        {
            var savedOslo = await db.Offices
                .AsNoTracking()
                .SingleAsync(x => x.Id == oslo.Id);

            var savedBengaluru = await db.Offices
                .AsNoTracking()
                .SingleAsync(x => x.Id == bengaluru.Id);

            Assert.Equal(amit.Id, savedOslo.LastAssignedVolunteerId);
            Assert.Equal(rahul.Id, savedBengaluru.LastAssignedVolunteerId);
        }

        await connection.DisposeAsync();
    }
}
