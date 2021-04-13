using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class IPTVChanneFk : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TVChannels_TVSources_TvSourceId",
                table: "TVChannels");

            migrationBuilder.DropIndex(
                name: "IX_TVChannels_TvSourceId",
                table: "TVChannels");

            migrationBuilder.DropColumn(
                name: "TvSourceId",
                table: "TVChannels");

            migrationBuilder.AddColumn<int>(
                name: "IdTvSource",
                table: "TVChannels",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TVChannels_IdTvSource",
                table: "TVChannels",
                column: "IdTvSource");

            migrationBuilder.AddForeignKey(
                name: "FK_TVChannels_TVSources_IdTvSource",
                table: "TVChannels",
                column: "IdTvSource",
                principalTable: "TVSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TVChannels_TVSources_IdTvSource",
                table: "TVChannels");

            migrationBuilder.DropIndex(
                name: "IX_TVChannels_IdTvSource",
                table: "TVChannels");

            migrationBuilder.DropColumn(
                name: "IdTvSource",
                table: "TVChannels");

            migrationBuilder.AddColumn<int>(
                name: "TvSourceId",
                table: "TVChannels",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TVChannels_TvSourceId",
                table: "TVChannels",
                column: "TvSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_TVChannels_TVSources_TvSourceId",
                table: "TVChannels",
                column: "TvSourceId",
                principalTable: "TVSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
