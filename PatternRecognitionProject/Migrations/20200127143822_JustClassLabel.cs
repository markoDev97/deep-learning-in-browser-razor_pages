using Microsoft.EntityFrameworkCore.Migrations;

namespace PatternRecognitionProject.Migrations
{
    public partial class JustClassLabel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "DataUnit");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "DataUnit",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
