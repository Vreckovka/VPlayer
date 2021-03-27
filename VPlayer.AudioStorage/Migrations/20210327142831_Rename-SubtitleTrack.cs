using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class RenameSubtitleTrack : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Subtitles",
                table: "TvShowEpisodes",
                newName: "SubtitleTrack");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubtitleTrack",
                table: "TvShowEpisodes",
                newName: "Subtitles");
        }
    }
}
