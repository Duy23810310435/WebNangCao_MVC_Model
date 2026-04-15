using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebNangCao_MVC_Model.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminFixReal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "PasswordHash", "Role", "Username" },
                values: new object[] { 1, new DateTime(2026, 4, 15, 0, 0, 0, 0, DateTimeKind.Utc), "admin123@gmail.com", "SuperAdmin", "$2a$12$y92vwbAsONQcJkeBGvrvP.W0Np6VHv2ouFiAeSkpLFC9iAcHzp2.q", "Admin", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
