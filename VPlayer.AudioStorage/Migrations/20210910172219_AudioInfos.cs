using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class AudioInfos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoundItems_SoundFileInfo_FileInfoId",
                table: "SoundItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SoundFileInfo",
                table: "SoundFileInfo");

            migrationBuilder.RenameTable(
                name: "SoundFileInfo",
                newName: "SoundFileInfos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SoundFileInfos",
                table: "SoundFileInfos",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SoundItems_SoundFileInfos_FileInfoId",
                table: "SoundItems",
                column: "FileInfoId",
                principalTable: "SoundFileInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoundItems_SoundFileInfos_FileInfoId",
                table: "SoundItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SoundFileInfos",
                table: "SoundFileInfos");

            migrationBuilder.RenameTable(
                name: "SoundFileInfos",
                newName: "SoundFileInfo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SoundFileInfo",
                table: "SoundFileInfo",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SoundItems_SoundFileInfo_FileInfoId",
                table: "SoundItems",
                column: "FileInfoId",
                principalTable: "SoundFileInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
