using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Web.Migrations
{
    /// <inheritdoc />
    public partial class anynews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaPath",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaPath",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaPath",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "MediaPath",
                table: "Assignments");
        }
    }
}
