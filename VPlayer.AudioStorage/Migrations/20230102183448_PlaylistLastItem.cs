using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class PlaylistLastItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActualItemId",
                table: "TvShowPlaylists",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdActualItem",
                table: "TvShowPlaylists",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActualItemId",
                table: "TvPlaylists",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdActualItem",
                table: "TvPlaylists",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActualItemId",
                table: "SongPlaylists",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdActualItem",
                table: "SongPlaylists",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TvShowPlaylists_ActualItemId",
                table: "TvShowPlaylists",
                column: "ActualItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TvPlaylists_ActualItemId",
                table: "TvPlaylists",
                column: "ActualItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SongPlaylists_ActualItemId",
                table: "SongPlaylists",
                column: "ActualItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SongPlaylists_PlaylistSongs_ActualItemId",
                table: "SongPlaylists",
                column: "ActualItemId",
                principalTable: "PlaylistSongs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TvPlaylists_TvPlaylistItem_ActualItemId",
                table: "TvPlaylists",
                column: "ActualItemId",
                principalTable: "TvPlaylistItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TvShowPlaylists_PlaylistsTvShowEpisode_ActualItemId",
                table: "TvShowPlaylists",
                column: "ActualItemId",
                principalTable: "PlaylistsTvShowEpisode",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SongPlaylists_PlaylistSongs_ActualItemId",
                table: "SongPlaylists");

            migrationBuilder.DropForeignKey(
                name: "FK_TvPlaylists_TvPlaylistItem_ActualItemId",
                table: "TvPlaylists");

            migrationBuilder.DropForeignKey(
                name: "FK_TvShowPlaylists_PlaylistsTvShowEpisode_ActualItemId",
                table: "TvShowPlaylists");

            migrationBuilder.DropIndex(
                name: "IX_TvShowPlaylists_ActualItemId",
                table: "TvShowPlaylists");

            migrationBuilder.DropIndex(
                name: "IX_TvPlaylists_ActualItemId",
                table: "TvPlaylists");

            migrationBuilder.DropIndex(
                name: "IX_SongPlaylists_ActualItemId",
                table: "SongPlaylists");

            migrationBuilder.DropColumn(
                name: "ActualItemId",
                table: "TvShowPlaylists");

            migrationBuilder.DropColumn(
                name: "IdActualItem",
                table: "TvShowPlaylists");

            migrationBuilder.DropColumn(
                name: "ActualItemId",
                table: "TvPlaylists");

            migrationBuilder.DropColumn(
                name: "IdActualItem",
                table: "TvPlaylists");

            migrationBuilder.DropColumn(
                name: "ActualItemId",
                table: "SongPlaylists");

            migrationBuilder.DropColumn(
                name: "IdActualItem",
                table: "SongPlaylists");
        }
    }
}
