using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnackCounter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficeRotationVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RotationVersion",
                table: "Offices",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RotationVersion",
                table: "Offices");
        }
    }
}
