using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class PlaylistCoverPath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverPath",
                table: "TvShowPlaylists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverPath",
                table: "TvPlaylists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverPath",
                table: "SongPlaylists",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverPath",
                table: "TvShowPlaylists");

            migrationBuilder.DropColumn(
                name: "CoverPath",
                table: "TvPlaylists");

            migrationBuilder.DropColumn(
                name: "CoverPath",
                table: "SongPlaylists");
        }
    }
}
