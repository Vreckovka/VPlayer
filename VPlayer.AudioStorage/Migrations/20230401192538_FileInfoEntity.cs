using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class FileInfoEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoundItems_SoundFileInfos_FileInfoId",
                table: "SoundItems");

            migrationBuilder.RenameColumn(
                name: "FileInfoId",
                table: "SoundItems",
                newName: "FileInfoEntityId");

            migrationBuilder.RenameIndex(
                name: "IX_SoundItems_FileInfoId",
                table: "SoundItems",
                newName: "IX_SoundItems_FileInfoEntityId");

            migrationBuilder.AddColumn<int>(
                name: "FileInfoEntityId",
                table: "VideoItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoItems_FileInfoEntityId",
                table: "VideoItems",
                column: "FileInfoEntityId");

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SoundItems_SoundFileInfos_FileInfoEntityId",
                table: "SoundItems");

            migrationBuilder.DropForeignKey(
                name: "FK_VideoItems_SoundFileInfos_FileInfoEntityId",
                table: "VideoItems");

            migrationBuilder.DropIndex(
                name: "IX_VideoItems_FileInfoEntityId",
                table: "VideoItems");

            migrationBuilder.DropColumn(
                name: "FileInfoEntityId",
                table: "VideoItems");

            migrationBuilder.RenameColumn(
                name: "FileInfoEntityId",
                table: "SoundItems",
                newName: "FileInfoId");

            migrationBuilder.RenameIndex(
                name: "IX_SoundItems_FileInfoEntityId",
                table: "SoundItems",
                newName: "IX_SoundItems_FileInfoId");

            migrationBuilder.AddForeignKey(
                name: "FK_SoundItems_SoundFileInfos_FileInfoId",
                table: "SoundItems",
                column: "FileInfoId",
                principalTable: "SoundFileInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
