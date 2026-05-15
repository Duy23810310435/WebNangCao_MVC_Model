using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebNangCao_MVC_Model.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_IsActive_CreatedAt",
                table: "Users",
                columns: new[] { "Role", "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Exams_CreatedAt",
                table: "Exams",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Role_IsActive_CreatedAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Exams_CreatedAt",
                table: "Exams");
        }
    }
}
