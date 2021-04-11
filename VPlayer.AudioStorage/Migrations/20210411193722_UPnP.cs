using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class UPnP : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UPnPDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceTypeText = table.Column<string>(type: "TEXT", nullable: true),
                    UDN = table.Column<string>(type: "TEXT", nullable: true),
                    FriendlyName = table.Column<string>(type: "TEXT", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerURL = table.Column<string>(type: "TEXT", nullable: true),
                    ModelName = table.Column<string>(type: "TEXT", nullable: true),
                    ModelURL = table.Column<string>(type: "TEXT", nullable: true),
                    ModelDescription = table.Column<string>(type: "TEXT", nullable: true),
                    ModelNumber = table.Column<string>(type: "TEXT", nullable: true),
                    SerialNumber = table.Column<string>(type: "TEXT", nullable: true),
                    UPC = table.Column<string>(type: "TEXT", nullable: true),
                    PresentationURL = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UPnPDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UPnPMediaServers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AliasURL = table.Column<string>(type: "TEXT", nullable: true),
                    PresentationURL = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultIconUrl = table.Column<string>(type: "TEXT", nullable: true),
                    OnlineServer = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContentDirectoryControlUrl = table.Column<string>(type: "TEXT", nullable: true),
                    UPnPDeviceId = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UPnPMediaServers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UPnPMediaServers_UPnPDevices_UPnPDeviceId",
                        column: x => x.UPnPDeviceId,
                        principalTable: "UPnPDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UPnPMediaServers_UPnPDeviceId",
                table: "UPnPMediaServers",
                column: "UPnPDeviceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UPnPMediaServers");

            migrationBuilder.DropTable(
                name: "UPnPDevices");
        }
    }
}
