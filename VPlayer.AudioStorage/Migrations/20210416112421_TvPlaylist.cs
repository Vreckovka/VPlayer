using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class TvPlaylist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TvPlaylists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    HashCode = table.Column<long>(type: "INTEGER", nullable: true),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: true),
                    LastItemIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPlayedTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    LastPlayed = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsUserCreated = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvPlaylists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TvPlaylistItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdTvChannel = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    TvPlaylistId = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvPlaylistItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TvPlaylistItem_TvChannels_IdTvChannel",
                        column: x => x.IdTvChannel,
                        principalTable: "TvChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TvPlaylistItem_TvPlaylists_TvPlaylistId",
                        column: x => x.TvPlaylistId,
                        principalTable: "TvPlaylists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TvPlaylistItem_IdTvChannel",
                table: "TvPlaylistItem",
                column: "IdTvChannel");

            migrationBuilder.CreateIndex(
                name: "IX_TvPlaylistItem_TvPlaylistId",
                table: "TvPlaylistItem",
                column: "TvPlaylistId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TvPlaylistItem");

            migrationBuilder.DropTable(
                name: "TvPlaylists");
        }
    }
}
