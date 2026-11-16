using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SnackCounter.Infrastructure.Data;

#nullable disable

namespace SnackCounter.Infrastructure.Migrations;

[Migration("20261116103000_AddRefillRequestConcurrencyVersion")]
public partial class AddRefillRequestConcurrencyVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "ConcurrencyVersion",
            table: "RefillRequests",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ConcurrencyVersion",
            table: "RefillRequests");
    }
}
