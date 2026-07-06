using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class VizualizationColors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HighVizualizationColor",
                table: "Albums",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LowVizualizationColor",
                table: "Albums",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MidVizualizationColor",
                table: "Albums",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HighVizualizationColor",
                table: "Albums");

            migrationBuilder.DropColumn(
                name: "LowVizualizationColor",
                table: "Albums");

            migrationBuilder.DropColumn(
                name: "MidVizualizationColor",
                table: "Albums");
        }
    }
}
