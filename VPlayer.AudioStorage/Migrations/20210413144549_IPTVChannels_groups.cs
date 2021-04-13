using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class IPTVChannels_groups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TVChannels_TVSources_TVSourceId",
                table: "TVChannels");

            migrationBuilder.RenameColumn(
                name: "TVSourceId",
                table: "TVChannels",
                newName: "TVChannelGroupsId");

            migrationBuilder.RenameIndex(
                name: "IX_TVChannels_TVSourceId",
                table: "TVChannels",
                newName: "IX_TVChannels_TVChannelGroupsId");

            migrationBuilder.AddColumn<string>(
                name: "UniqueIndetifier",
                table: "TVChannels",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TVChannelGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    TVSourceId = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TVChannelGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TVChannelGroups_TVSources_TVSourceId",
                        column: x => x.TVSourceId,
                        principalTable: "TVSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TVChannelGroups_TVSourceId",
                table: "TVChannelGroups",
                column: "TVSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_TVChannels_TVChannelGroups_TVChannelGroupsId",
                table: "TVChannels",
                column: "TVChannelGroupsId",
                principalTable: "TVChannelGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TVChannels_TVChannelGroups_TVChannelGroupsId",
                table: "TVChannels");

            migrationBuilder.DropTable(
                name: "TVChannelGroups");

            migrationBuilder.DropColumn(
                name: "UniqueIndetifier",
                table: "TVChannels");

            migrationBuilder.RenameColumn(
                name: "TVChannelGroupsId",
                table: "TVChannels",
                newName: "TVSourceId");

            migrationBuilder.RenameIndex(
                name: "IX_TVChannels_TVChannelGroupsId",
                table: "TVChannels",
                newName: "IX_TVChannels_TVSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_TVChannels_TVSources_TVSourceId",
                table: "TVChannels",
                column: "TVSourceId",
                principalTable: "TVSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
