using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebNangCao_MVC_Model.Migrations
{
    /// <inheritdoc />
    public partial class AddDifficultyToExam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "Exams",
                type: "text",
                nullable: false,
                defaultValue: "Trung bình");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Exams");
        }
    }
}
