using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class RemovedColumns_2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Length",
                table: "SoundItems");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "SoundItems");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "SoundItems");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Songs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Length",
                table: "SoundItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SoundItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "SoundItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Songs",
                type: "TEXT",
                nullable: true);
        }
    }
}
