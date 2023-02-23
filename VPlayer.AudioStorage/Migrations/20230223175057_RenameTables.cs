using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class RenameTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistSongs_SongPlaylists_SoundItemFilePlaylistId",
                table: "PlaylistSongs");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShowPlaylists_VideoFilePlaylistId",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.DropForeignKey(
                name: "FK_SongPlaylists_PlaylistSongs_ActualItemId",
                table: "SongPlaylists");

            migrationBuilder.DropForeignKey(
                name: "FK_TvShowPlaylists_PlaylistsTvShowEpisode_ActualItemId",
                table: "TvShowPlaylists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TvShowPlaylists",
                table: "TvShowPlaylists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SongPlaylists",
                table: "SongPlaylists");

            migrationBuilder.RenameTable(
                name: "TvShowPlaylists",
                newName: "VideoFilePlaylists");

            migrationBuilder.RenameTable(
                name: "SongPlaylists",
                newName: "SoundItemPlaylists");

            migrationBuilder.RenameIndex(
                name: "IX_TvShowPlaylists_HashCode_IsUserCreated",
                table: "VideoFilePlaylists",
                newName: "IX_VideoFilePlaylists_HashCode_IsUserCreated");

            migrationBuilder.RenameIndex(
                name: "IX_TvShowPlaylists_ActualItemId",
                table: "VideoFilePlaylists",
                newName: "IX_VideoFilePlaylists_ActualItemId");

            migrationBuilder.RenameIndex(
                name: "IX_SongPlaylists_HashCode_IsUserCreated",
                table: "SoundItemPlaylists",
                newName: "IX_SoundItemPlaylists_HashCode_IsUserCreated");

            migrationBuilder.RenameIndex(
                name: "IX_SongPlaylists_ActualItemId",
                table: "SoundItemPlaylists",
                newName: "IX_SoundItemPlaylists_ActualItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VideoFilePlaylists",
                table: "VideoFilePlaylists",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SoundItemPlaylists",
                table: "SoundItemPlaylists",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistSongs_SoundItemPlaylists_SoundItemFilePlaylistId",
                table: "PlaylistSongs",
                column: "SoundItemFilePlaylistId",
                principalTable: "SoundItemPlaylists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistsTvShowEpisode_VideoFilePlaylists_VideoFilePlaylistId",
                table: "PlaylistsTvShowEpisode",
                column: "VideoFilePlaylistId",
                principalTable: "VideoFilePlaylists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SoundItemPlaylists_PlaylistSongs_ActualItemId",
                table: "SoundItemPlaylists",
                column: "ActualItemId",
                principalTable: "PlaylistSongs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoFilePlaylists_PlaylistsTvShowEpisode_ActualItemId",
                table: "VideoFilePlaylists",
                column: "ActualItemId",
                principalTable: "PlaylistsTvShowEpisode",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistSongs_SoundItemPlaylists_SoundItemFilePlaylistId",
                table: "PlaylistSongs");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistsTvShowEpisode_VideoFilePlaylists_VideoFilePlaylistId",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.DropForeignKey(
                name: "FK_SoundItemPlaylists_PlaylistSongs_ActualItemId",
                table: "SoundItemPlaylists");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoFilePlaylists_PlaylistsTvShowEpisode_ActualItemId",
                table: "VideoFilePlaylists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VideoFilePlaylists",
                table: "VideoFilePlaylists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SoundItemPlaylists",
                table: "SoundItemPlaylists");

            migrationBuilder.RenameTable(
                name: "VideoFilePlaylists",
                newName: "TvShowPlaylists");

            migrationBuilder.RenameTable(
                name: "SoundItemPlaylists",
                newName: "SongPlaylists");

            migrationBuilder.RenameIndex(
                name: "IX_VideoFilePlaylists_HashCode_IsUserCreated",
                table: "TvShowPlaylists",
                newName: "IX_TvShowPlaylists_HashCode_IsUserCreated");

            migrationBuilder.RenameIndex(
                name: "IX_VideoFilePlaylists_ActualItemId",
                table: "TvShowPlaylists",
                newName: "IX_TvShowPlaylists_ActualItemId");

            migrationBuilder.RenameIndex(
                name: "IX_SoundItemPlaylists_HashCode_IsUserCreated",
                table: "SongPlaylists",
                newName: "IX_SongPlaylists_HashCode_IsUserCreated");

            migrationBuilder.RenameIndex(
                name: "IX_SoundItemPlaylists_ActualItemId",
                table: "SongPlaylists",
                newName: "IX_SongPlaylists_ActualItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TvShowPlaylists",
                table: "TvShowPlaylists",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SongPlaylists",
                table: "SongPlaylists",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistSongs_SongPlaylists_SoundItemFilePlaylistId",
                table: "PlaylistSongs",
                column: "SoundItemFilePlaylistId",
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

            migrationBuilder.AddForeignKey(
                name: "FK_SongPlaylists_PlaylistSongs_ActualItemId",
                table: "SongPlaylists",
                column: "ActualItemId",
                principalTable: "PlaylistSongs",
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
    }
}
