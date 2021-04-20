using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class MediaRenderers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UPnPMediaRenderers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PresentationURL = table.Column<string>(type: "TEXT", nullable: true),
                    UPnPDeviceId = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UPnPMediaRenderers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UPnPMediaRenderers_UPnPDevices_UPnPDeviceId",
                        column: x => x.UPnPDeviceId,
                        principalTable: "UPnPDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UPnPMediaRenderers_UPnPDeviceId",
                table: "UPnPMediaRenderers",
                column: "UPnPDeviceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UPnPMediaRenderers");
        }
    }
}
