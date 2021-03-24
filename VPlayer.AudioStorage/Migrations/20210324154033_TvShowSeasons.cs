using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class TvShowSeasons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeasonNumber",
                table: "TvShowEpisodes");

            migrationBuilder.AddColumn<int>(
                name: "TvShowSeasonId",
                table: "TvShowEpisodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TvShowsSeasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TvShowId = table.Column<int>(type: "INTEGER", nullable: true),
                    SeasonNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CsfdUrl = table.Column<string>(type: "TEXT", nullable: true),
                    PosterPath = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    InfoDownloadStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvShowsSeasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TvShowsSeasons_TvShows_TvShowId",
                        column: x => x.TvShowId,
                        principalTable: "TvShows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TvShowEpisodes_TvShowSeasonId",
                table: "TvShowEpisodes",
                column: "TvShowSeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_TvShowsSeasons_TvShowId",
                table: "TvShowsSeasons",
                column: "TvShowId");

            migrationBuilder.AddForeignKey(
                name: "FK_TvShowEpisodes_TvShowsSeasons_TvShowSeasonId",
                table: "TvShowEpisodes",
                column: "TvShowSeasonId",
                principalTable: "TvShowsSeasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TvShowEpisodes_TvShowsSeasons_TvShowSeasonId",
                table: "TvShowEpisodes");

            migrationBuilder.DropTable(
                name: "TvShowsSeasons");

            migrationBuilder.DropIndex(
                name: "IX_TvShowEpisodes_TvShowSeasonId",
                table: "TvShowEpisodes");

            migrationBuilder.DropColumn(
                name: "TvShowSeasonId",
                table: "TvShowEpisodes");

            migrationBuilder.AddColumn<int>(
                name: "SeasonNumber",
                table: "TvShowEpisodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
