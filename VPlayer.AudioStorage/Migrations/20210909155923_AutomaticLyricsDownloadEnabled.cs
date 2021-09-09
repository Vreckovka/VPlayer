using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class AutomaticLyricsDownloadEnabled : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutomaticLyricsFindEnabled",
                table: "SoundItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAutomaticLyricsFindEnabled",
                table: "SoundItems");
        }
    }
}
