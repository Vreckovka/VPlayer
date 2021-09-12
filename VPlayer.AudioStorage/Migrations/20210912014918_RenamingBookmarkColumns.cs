using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class RenamingBookmarkColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Source",
                table: "Bookmarks",
                newName: "Path");

            migrationBuilder.RenameColumn(
                name: "Indentificator",
                table: "Bookmarks",
                newName: "Identificator");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Path",
                table: "Bookmarks",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "Identificator",
                table: "Bookmarks",
                newName: "Indentificator");
        }
    }
}
