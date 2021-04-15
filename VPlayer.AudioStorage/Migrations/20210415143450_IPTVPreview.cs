using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class IPTVPreview : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistSongs_SongPlaylists_SongsPlaylistId",
                table: "PlaylistSongs");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShowPlaylists_VideoPlaylistId",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.RenameColumn(
                name: "DiskLocation",
                table: "VideoItems",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "DiskLocation",
                table: "TvShowEpisodes",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "DiskLocation",
                table: "Songs",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "VideoPlaylistId",
                table: "PlaylistsTvShowEpisode",
                newName: "VideoFilePlaylistId");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistsTvShowEpisode_VideoPlaylistId",
                table: "PlaylistsTvShowEpisode",
                newName: "IX_PlaylistsTvShowEpisode_VideoFilePlaylistId");

            migrationBuilder.RenameColumn(
                name: "SongsPlaylistId",
                table: "PlaylistSongs",
                newName: "SongsFilePlaylistId");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistSongs_SongsPlaylistId",
                table: "PlaylistSongs",
                newName: "IX_PlaylistSongs_SongsFilePlaylistId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistSongs_SongPlaylists_SongsFilePlaylistId",
                table: "PlaylistSongs",
                column: "SongsFilePlaylistId",
                principalTable: "SongPlaylists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShowPlaylists_VideoFilePlaylistId",
                table: "PlaylistsTvShowEpisode",
                column: "VideoFilePlaylistId",
                principalTable: "TvShowPlaylists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistSongs_SongPlaylists_SongsFilePlaylistId",
                table: "PlaylistSongs");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShowPlaylists_VideoFilePlaylistId",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "VideoItems",
                newName: "DiskLocation");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "TvShowEpisodes",
                newName: "DiskLocation");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "Songs",
                newName: "DiskLocation");

            migrationBuilder.RenameColumn(
                name: "VideoFilePlaylistId",
                table: "PlaylistsTvShowEpisode",
                newName: "VideoPlaylistId");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistsTvShowEpisode_VideoFilePlaylistId",
                table: "PlaylistsTvShowEpisode",
                newName: "IX_PlaylistsTvShowEpisode_VideoPlaylistId");

            migrationBuilder.RenameColumn(
                name: "SongsFilePlaylistId",
                table: "PlaylistSongs",
                newName: "SongsPlaylistId");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistSongs_SongsFilePlaylistId",
                table: "PlaylistSongs",
                newName: "IX_PlaylistSongs_SongsPlaylistId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistSongs_SongPlaylists_SongsPlaylistId",
                table: "PlaylistSongs",
                column: "SongsPlaylistId",
                principalTable: "SongPlaylists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShowPlaylists_VideoPlaylistId",
                table: "PlaylistsTvShowEpisode",
                column: "VideoPlaylistId",
                principalTable: "TvShowPlaylists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
