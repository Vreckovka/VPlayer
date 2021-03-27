using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class TvShowReference : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShows_TvShowId",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.DropIndex(
                name: "IX_PlaylistsTvShowEpisode_TvShowId",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.DropColumn(
                name: "TvShowId",
                table: "PlaylistsTvShowEpisode");

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
                name: "FK_PlaylistsTvShowEpisode_TvShows_IdTvShow",
                table: "PlaylistsTvShowEpisode",
                column: "IdTvShow",
                principalTable: "TvShows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShows_IdTvShow",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.DropIndex(
                name: "IX_PlaylistsTvShowEpisode_IdTvShow",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.DropColumn(
                name: "IdTvShow",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.AddColumn<int>(
                name: "TvShowId",
                table: "PlaylistsTvShowEpisode",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistsTvShowEpisode_TvShowId",
                table: "PlaylistsTvShowEpisode",
                column: "TvShowId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistsTvShowEpisode_TvShows_TvShowId",
                table: "PlaylistsTvShowEpisode",
                column: "TvShowId",
                principalTable: "TvShows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
