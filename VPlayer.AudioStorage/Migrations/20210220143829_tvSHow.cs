using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class tvSHow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
