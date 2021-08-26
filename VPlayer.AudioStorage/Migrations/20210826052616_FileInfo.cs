using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class FileInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FileInfoId",
                table: "SoundItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SoundFileInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Artist = table.Column<string>(type: "TEXT", nullable: true),
                    Album = table.Column<string>(type: "TEXT", nullable: true),
                    Indentificator = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    FullName = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Length = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoundFileInfo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SoundItems_FileInfoId",
                table: "SoundItems",
                column: "FileInfoId");

            migrationBuilder.AddForeignKey(
                name: "FK_SoundItems_SoundFileInfo_FileInfoId",
                table: "SoundItems",
                column: "FileInfoId",
                principalTable: "SoundFileInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoundItems_SoundFileInfo_FileInfoId",
                table: "SoundItems");

            migrationBuilder.DropTable(
                name: "SoundFileInfo");

            migrationBuilder.DropIndex(
                name: "IX_SoundItems_FileInfoId",
                table: "SoundItems");

            migrationBuilder.DropColumn(
                name: "FileInfoId",
                table: "SoundItems");
        }
    }
}
