using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class FileInfos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoundItems_SoundFileInfos_FileInfoEntityId",
                table: "SoundItems");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoItems_SoundFileInfos_FileInfoEntityId",
                table: "VideoItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SoundFileInfos",
                table: "SoundFileInfos");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "VideoItems");

            migrationBuilder.RenameTable(
                name: "SoundFileInfos",
                newName: "FileInfos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FileInfos",
                table: "FileInfos",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SoundItems_FileInfos_FileInfoEntityId",
                table: "SoundItems",
                column: "FileInfoEntityId",
                principalTable: "FileInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoItems_FileInfos_FileInfoEntityId",
                table: "VideoItems",
                column: "FileInfoEntityId",
                principalTable: "FileInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoundItems_FileInfos_FileInfoEntityId",
                table: "SoundItems");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoItems_FileInfos_FileInfoEntityId",
                table: "VideoItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FileInfos",
                table: "FileInfos");

            migrationBuilder.RenameTable(
                name: "FileInfos",
                newName: "SoundFileInfos");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "VideoItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SoundFileInfos",
                table: "SoundFileInfos",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SoundItems_SoundFileInfos_FileInfoEntityId",
                table: "SoundItems",
                column: "FileInfoEntityId",
                principalTable: "SoundFileInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VideoItems_SoundFileInfos_FileInfoEntityId",
                table: "VideoItems",
                column: "FileInfoEntityId",
                principalTable: "SoundFileInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
