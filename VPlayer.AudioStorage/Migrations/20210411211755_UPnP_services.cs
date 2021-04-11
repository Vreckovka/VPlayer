using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class UPnP_services : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UPnPServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceType = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceId = table.Column<string>(type: "TEXT", nullable: true),
                    SCPDURL = table.Column<string>(type: "TEXT", nullable: true),
                    EventSubURL = table.Column<string>(type: "TEXT", nullable: true),
                    ControlURL = table.Column<string>(type: "TEXT", nullable: true),
                    UPnPDeviceId = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UPnPServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UPnPServices_UPnPDevices_UPnPDeviceId",
                        column: x => x.UPnPDeviceId,
                        principalTable: "UPnPDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UPnPServices_UPnPDeviceId",
                table: "UPnPServices",
                column: "UPnPDeviceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UPnPServices");
        }
    }
}
