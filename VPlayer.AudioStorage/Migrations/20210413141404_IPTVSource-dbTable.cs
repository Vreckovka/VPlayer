using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class IPTVSourcedbTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UPnPMediaServer_UPnPDevices_UPnPDeviceId",
                table: "UPnPMediaServer");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UPnPMediaServer",
                table: "UPnPMediaServer");

            migrationBuilder.RenameTable(
                name: "UPnPMediaServer",
                newName: "UPnPMediaServers");

            migrationBuilder.RenameIndex(
                name: "IX_UPnPMediaServer_UPnPDeviceId",
                table: "UPnPMediaServers",
                newName: "IX_UPnPMediaServers_UPnPDeviceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UPnPMediaServers",
                table: "UPnPMediaServers",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "TVSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    TvSourceType = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceConnection = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TVSources", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_UPnPMediaServers_UPnPDevices_UPnPDeviceId",
                table: "UPnPMediaServers",
                column: "UPnPDeviceId",
                principalTable: "UPnPDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UPnPMediaServers_UPnPDevices_UPnPDeviceId",
                table: "UPnPMediaServers");

            migrationBuilder.DropTable(
                name: "TVSources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UPnPMediaServers",
                table: "UPnPMediaServers");

            migrationBuilder.RenameTable(
                name: "UPnPMediaServers",
                newName: "UPnPMediaServer");

            migrationBuilder.RenameIndex(
                name: "IX_UPnPMediaServers_UPnPDeviceId",
                table: "UPnPMediaServer",
                newName: "IX_UPnPMediaServer_UPnPDeviceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UPnPMediaServer",
                table: "UPnPMediaServer",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UPnPMediaServer_UPnPDevices_UPnPDeviceId",
                table: "UPnPMediaServer",
                column: "UPnPDeviceId",
                principalTable: "UPnPDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
