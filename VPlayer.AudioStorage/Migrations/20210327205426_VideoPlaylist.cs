using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class VideoPlaylist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShowEpisodes_IdTvShowEpisode",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShowPlaylists_TvShowPlaylistId",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShows_IdTvShow",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.DropIndex(
                name: "IX_PlaylistsTvShowEpisode_IdTvShow",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.DropColumn(
                name: "IdTvShow",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.RenameColumn(
                name: "TvShowPlaylistId",
                table: "PlaylistsTvShowEpisode",
                newName: "VideoPlaylistId");

            migrationBuilder.RenameColumn(
                name: "IdTvShowEpisode",
                table: "PlaylistsTvShowEpisode",
                newName: "IdVideoItem");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistsTvShowEpisode_TvShowPlaylistId",
                table: "PlaylistsTvShowEpisode",
                newName: "IX_PlaylistsTvShowEpisode_VideoPlaylistId");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistsTvShowEpisode_IdTvShowEpisode",
                table: "PlaylistsTvShowEpisode",
                newName: "IX_PlaylistsTvShowEpisode_IdVideoItem");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShowPlaylists_VideoPlaylistId",
                table: "PlaylistsTvShowEpisode",
                column: "VideoPlaylistId",
                principalTable: "TvShowPlaylists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistsTvShowEpisode_VideoItems_IdVideoItem",
                table: "PlaylistsTvShowEpisode",
                column: "IdVideoItem",
                principalTable: "VideoItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShowPlaylists_VideoPlaylistId",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistsTvShowEpisode_VideoItems_IdVideoItem",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.RenameColumn(
                name: "VideoPlaylistId",
                table: "PlaylistsTvShowEpisode",
                newName: "TvShowPlaylistId");

            migrationBuilder.RenameColumn(
                name: "IdVideoItem",
                table: "PlaylistsTvShowEpisode",
                newName: "IdTvShowEpisode");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistsTvShowEpisode_VideoPlaylistId",
                table: "PlaylistsTvShowEpisode",
                newName: "IX_PlaylistsTvShowEpisode_TvShowPlaylistId");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistsTvShowEpisode_IdVideoItem",
                table: "PlaylistsTvShowEpisode",
                newName: "IX_PlaylistsTvShowEpisode_IdTvShowEpisode");

            migrationBuilder.AddColumn<int>(
                name: "IdTvShow",
                table: "PlaylistsTvShowEpisode",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistsTvShowEpisode_IdTvShow",
                table: "PlaylistsTvShowEpisode",
                column: "IdTvShow");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShowEpisodes_IdTvShowEpisode",
                table: "PlaylistsTvShowEpisode",
                column: "IdTvShowEpisode",
                principalTable: "TvShowEpisodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShowPlaylists_TvShowPlaylistId",
                table: "PlaylistsTvShowEpisode",
                column: "TvShowPlaylistId",
                principalTable: "TvShowPlaylists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShows_IdTvShow",
                table: "PlaylistsTvShowEpisode",
                column: "IdTvShow",
                principalTable: "TvShows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
