using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SnackCounter.Infrastructure.Data;

#nullable disable

namespace SnackCounter.Infrastructure.Migrations;

[Migration("20261116100000_AddPurchaseLineItems")]
public partial class AddPurchaseLineItems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Quantity", table: "Purchases", type: "INTEGER",
            nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<decimal>(
            name: "UnitPrice", table: "Purchases", type: "TEXT",
            precision: 18, scale: 2, nullable: false, defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Quantity", table: "Purchases");
        migrationBuilder.DropColumn(name: "UnitPrice", table: "Purchases");
    }
}
