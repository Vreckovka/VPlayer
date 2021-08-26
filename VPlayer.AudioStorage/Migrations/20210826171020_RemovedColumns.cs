using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class RemovedColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Songs_SoundItems_SoundItemId",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Songs");

            migrationBuilder.RenameColumn(
                name: "SoundItemId",
                table: "Songs",
                newName: "ItemModelId");

            migrationBuilder.RenameIndex(
                name: "IX_Songs_SoundItemId",
                table: "Songs",
                newName: "IX_Songs_ItemModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_SoundItems_ItemModelId",
                table: "Songs",
                column: "ItemModelId",
                principalTable: "SoundItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Songs_SoundItems_ItemModelId",
                table: "Songs");

            migrationBuilder.RenameColumn(
                name: "ItemModelId",
                table: "Songs",
                newName: "SoundItemId");

            migrationBuilder.RenameIndex(
                name: "IX_Songs_ItemModelId",
                table: "Songs",
                newName: "IX_Songs_SoundItemId");

            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "Songs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Songs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "Length",
                table: "Songs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Songs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Songs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_SoundItems_SoundItemId",
                table: "Songs",
                column: "SoundItemId",
                principalTable: "SoundItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
