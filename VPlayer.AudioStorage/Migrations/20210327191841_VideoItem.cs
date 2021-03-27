using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class VideoItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AspectRatio",
                table: "TvShowEpisodes");

            migrationBuilder.DropColumn(
                name: "AudioTrack",
                table: "TvShowEpisodes");

            migrationBuilder.RenameColumn(
                name: "SubtitleTrack",
                table: "TvShowEpisodes",
                newName: "VideoItemId");

            migrationBuilder.CreateTable(
                name: "VideoItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiskLocation = table.Column<string>(type: "TEXT", nullable: true),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    Length = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    AspectRatio = table.Column<string>(type: "TEXT", nullable: true),
                    AudioTrack = table.Column<int>(type: "INTEGER", nullable: true),
                    SubtitleTrack = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TvShowEpisodes_VideoItemId",
                table: "TvShowEpisodes",
                column: "VideoItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_TvShowEpisodes_VideoItems_VideoItemId",
                table: "TvShowEpisodes",
                column: "VideoItemId",
                principalTable: "VideoItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TvShowEpisodes_VideoItems_VideoItemId",
                table: "TvShowEpisodes");

            migrationBuilder.DropTable(
                name: "VideoItems");

            migrationBuilder.DropIndex(
                name: "IX_TvShowEpisodes_VideoItemId",
                table: "TvShowEpisodes");

            migrationBuilder.RenameColumn(
                name: "VideoItemId",
                table: "TvShowEpisodes",
                newName: "SubtitleTrack");

            migrationBuilder.AddColumn<string>(
                name: "AspectRatio",
                table: "TvShowEpisodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AudioTrack",
                table: "TvShowEpisodes",
                type: "INTEGER",
                nullable: true);
        }
    }
}
