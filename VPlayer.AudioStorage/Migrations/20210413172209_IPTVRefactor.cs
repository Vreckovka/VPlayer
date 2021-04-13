using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class IPTVRefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TVChannelGroups_TVSources_TVSourceId",
                table: "TVChannelGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_TVChannels_TVChannelGroups_TVChannelGroupsId",
                table: "TVChannels");

            migrationBuilder.DropIndex(
                name: "IX_TVChannelGroups_TVSourceId",
                table: "TVChannelGroups");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "TVChannels");

            migrationBuilder.DropColumn(
                name: "UniqueIndetifier",
                table: "TVChannels");

            migrationBuilder.DropColumn(
                name: "TVSourceId",
                table: "TVChannelGroups");

            migrationBuilder.RenameColumn(
                name: "TVChannelGroupsId",
                table: "TVChannels",
                newName: "TvSourceId");

            migrationBuilder.RenameIndex(
                name: "IX_TVChannels_TVChannelGroupsId",
                table: "TVChannels",
                newName: "IX_TVChannels_TvSourceId");

            migrationBuilder.AddColumn<int>(
                name: "TvChannelGroupId",
                table: "TVChannels",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TVChannels_TvChannelGroupId",
                table: "TVChannels",
                column: "TvChannelGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_TVChannels_TVChannelGroups_TvChannelGroupId",
                table: "TVChannels",
                column: "TvChannelGroupId",
                principalTable: "TVChannelGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TVChannels_TVSources_TvSourceId",
                table: "TVChannels",
                column: "TvSourceId",
                principalTable: "TVSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TVChannels_TVChannelGroups_TvChannelGroupId",
                table: "TVChannels");

            migrationBuilder.DropForeignKey(
                name: "FK_TVChannels_TVSources_TvSourceId",
                table: "TVChannels");

            migrationBuilder.DropIndex(
                name: "IX_TVChannels_TvChannelGroupId",
                table: "TVChannels");

            migrationBuilder.DropColumn(
                name: "TvChannelGroupId",
                table: "TVChannels");

            migrationBuilder.RenameColumn(
                name: "TvSourceId",
                table: "TVChannels",
                newName: "TVChannelGroupsId");

            migrationBuilder.RenameIndex(
                name: "IX_TVChannels_TvSourceId",
                table: "TVChannels",
                newName: "IX_TVChannels_TVChannelGroupsId");

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "TVChannels",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniqueIndetifier",
                table: "TVChannels",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TVSourceId",
                table: "TVChannelGroups",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TVChannelGroups_TVSourceId",
                table: "TVChannelGroups",
                column: "TVSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_TVChannelGroups_TVSources_TVSourceId",
                table: "TVChannelGroups",
                column: "TVSourceId",
                principalTable: "TVSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TVChannels_TVChannelGroups_TVChannelGroupsId",
                table: "TVChannels",
                column: "TVChannelGroupsId",
                principalTable: "TVChannelGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
