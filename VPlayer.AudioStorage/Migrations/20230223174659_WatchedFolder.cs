using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class WatchedFolder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WatchFolder",
                table: "TvShowPlaylists");

            migrationBuilder.DropColumn(
                name: "WatchFolder",
                table: "SongPlaylists");

            migrationBuilder.AddColumn<string>(
                name: "WatchedFolder",
                table: "TvShowPlaylists",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WatchedFolder",
                table: "SongPlaylists",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WatchedFolder",
                table: "TvShowPlaylists");

            migrationBuilder.DropColumn(
                name: "WatchedFolder",
                table: "SongPlaylists");

            migrationBuilder.AddColumn<bool>(
                name: "WatchFolder",
                table: "TvShowPlaylists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WatchFolder",
                table: "SongPlaylists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
