using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnackCounter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefillRequestRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RefillRequests_AssignedVolunteerId",
                table: "RefillRequests",
                column: "AssignedVolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_RefillRequests_ProductId",
                table: "RefillRequests",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_RefillRequests_Employees_AssignedVolunteerId",
                table: "RefillRequests",
                column: "AssignedVolunteerId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RefillRequests_Offices_OfficeId",
                table: "RefillRequests",
                column: "OfficeId",
                principalTable: "Offices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RefillRequests_Products_ProductId",
                table: "RefillRequests",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefillRequests_Employees_AssignedVolunteerId",
                table: "RefillRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RefillRequests_Offices_OfficeId",
                table: "RefillRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RefillRequests_Products_ProductId",
                table: "RefillRequests");

            migrationBuilder.DropIndex(
                name: "IX_RefillRequests_AssignedVolunteerId",
                table: "RefillRequests");

            migrationBuilder.DropIndex(
                name: "IX_RefillRequests_ProductId",
                table: "RefillRequests");
        }
    }
}
