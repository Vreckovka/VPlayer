using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class SoundVideo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoPath",
                table: "SoundItems",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoPath",
                table: "SoundItems");
        }
    }
}
