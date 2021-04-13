using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class IPTVChannel_1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TVSourceId",
                table: "TVChannels",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TVChannels_TVSourceId",
                table: "TVChannels",
                column: "TVSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_TVChannels_TVSources_TVSourceId",
                table: "TVChannels",
                column: "TVSourceId",
                principalTable: "TVSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TVChannels_TVSources_TVSourceId",
                table: "TVChannels");

            migrationBuilder.DropIndex(
                name: "IX_TVChannels_TVSourceId",
                table: "TVChannels");

            migrationBuilder.DropColumn(
                name: "TVSourceId",
                table: "TVChannels");
        }
    }
}
