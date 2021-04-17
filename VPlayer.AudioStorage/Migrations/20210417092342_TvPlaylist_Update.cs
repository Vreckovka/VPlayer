using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class TvPlaylist_Update : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TvPlaylistItem_TvChannels_IdTvChannel",
                table: "TvPlaylistItem");

            migrationBuilder.RenameColumn(
                name: "IdTvChannel",
                table: "TvPlaylistItem",
                newName: "IdTvChannelGroup");

            migrationBuilder.RenameIndex(
                name: "IX_TvPlaylistItem_IdTvChannel",
                table: "TvPlaylistItem",
                newName: "IX_TvPlaylistItem_IdTvChannelGroup");

            migrationBuilder.AddForeignKey(
                name: "FK_TvPlaylistItem_TvChannelGroups_IdTvChannelGroup",
                table: "TvPlaylistItem",
                column: "IdTvChannelGroup",
                principalTable: "TvChannelGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TvPlaylistItem_TvChannelGroups_IdTvChannelGroup",
                table: "TvPlaylistItem");

            migrationBuilder.RenameColumn(
                name: "IdTvChannelGroup",
                table: "TvPlaylistItem",
                newName: "IdTvChannel");

            migrationBuilder.RenameIndex(
                name: "IX_TvPlaylistItem_IdTvChannelGroup",
                table: "TvPlaylistItem",
                newName: "IX_TvPlaylistItem_IdTvChannel");

            migrationBuilder.AddForeignKey(
                name: "FK_TvPlaylistItem_TvChannels_IdTvChannel",
                table: "TvPlaylistItem",
                column: "IdTvChannel",
                principalTable: "TvChannels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
