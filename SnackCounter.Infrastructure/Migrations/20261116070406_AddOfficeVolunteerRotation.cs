using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnackCounter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficeVolunteerRotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LastAssignedVolunteerId",
                table: "Offices",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastAssignedVolunteerId",
                table: "Offices");
        }
    }
}
