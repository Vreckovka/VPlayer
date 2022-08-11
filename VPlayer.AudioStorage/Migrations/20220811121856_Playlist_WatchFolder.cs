using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class Playlist_WatchFolder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Songs_SoundItems_ItemModelId",
                table: "Songs");

            migrationBuilder.AddColumn<bool>(
                name: "WatchFolder",
                table: "TvShowPlaylists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "ItemModelId",
                table: "Songs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WatchFolder",
                table: "SongPlaylists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_SoundItems_ItemModelId",
                table: "Songs",
                column: "ItemModelId",
                principalTable: "SoundItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Songs_SoundItems_ItemModelId",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "WatchFolder",
                table: "TvShowPlaylists");

            migrationBuilder.DropColumn(
                name: "WatchFolder",
                table: "SongPlaylists");

            migrationBuilder.AlterColumn<int>(
                name: "ItemModelId",
                table: "Songs",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_SoundItems_ItemModelId",
                table: "Songs",
                column: "ItemModelId",
                principalTable: "SoundItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
