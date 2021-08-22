using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class SoundItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistSongs_SongPlaylists_SongsFilePlaylistId",
                table: "PlaylistSongs");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistSongs_Songs_IdReferencedItem",
                table: "PlaylistSongs");

            migrationBuilder.RenameColumn(
                name: "SongsFilePlaylistId",
                table: "PlaylistSongs",
                newName: "SoundItemFilePlaylistId");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistSongs_SongsFilePlaylistId",
                table: "PlaylistSongs",
                newName: "IX_PlaylistSongs_SoundItemFilePlaylistId");

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "Songs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Modified",
                table: "Songs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoundItemId",
                table: "Songs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SoundItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    Length = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoundItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Songs_SoundItemId",
                table: "Songs",
                column: "SoundItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistSongs_SongPlaylists_SoundItemFilePlaylistId",
                table: "PlaylistSongs",
                column: "SoundItemFilePlaylistId",
                principalTable: "SongPlaylists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistSongs_SoundItems_IdReferencedItem",
                table: "PlaylistSongs",
                column: "IdReferencedItem",
                principalTable: "SoundItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_SoundItems_SoundItemId",
                table: "Songs",
                column: "SoundItemId",
                principalTable: "SoundItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistSongs_SongPlaylists_SoundItemFilePlaylistId",
                table: "PlaylistSongs");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistSongs_SoundItems_IdReferencedItem",
                table: "PlaylistSongs");

            migrationBuilder.DropForeignKey(
                name: "FK_Songs_SoundItems_SoundItemId",
                table: "Songs");

            migrationBuilder.DropTable(
                name: "SoundItems");

            migrationBuilder.DropIndex(
                name: "IX_Songs_SoundItemId",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Modified",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "SoundItemId",
                table: "Songs");

            migrationBuilder.RenameColumn(
                name: "SoundItemFilePlaylistId",
                table: "PlaylistSongs",
                newName: "SongsFilePlaylistId");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistSongs_SoundItemFilePlaylistId",
                table: "PlaylistSongs",
                newName: "IX_PlaylistSongs_SongsFilePlaylistId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistSongs_SongPlaylists_SongsFilePlaylistId",
                table: "PlaylistSongs",
                column: "SongsFilePlaylistId",
                principalTable: "SongPlaylists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistSongs_Songs_IdReferencedItem",
                table: "PlaylistSongs",
                column: "IdReferencedItem",
                principalTable: "Songs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
