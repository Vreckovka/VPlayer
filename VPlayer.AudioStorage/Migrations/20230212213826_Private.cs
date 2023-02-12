using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class Private : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeSpan>(
                name: "TimePlayed",
                table: "VideoItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "VideoItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "TvShowPlaylists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "TvShowEpisodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimePlayed",
                table: "TvShowEpisodes",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "TvPlaylists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "TvItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimePlayed",
                table: "TvItem",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "TimePlayed",
                table: "SoundItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "SoundItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "SongPlaylists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "VideoItems");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "TvShowPlaylists");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "TvShowEpisodes");

            migrationBuilder.DropColumn(
                name: "TimePlayed",
                table: "TvShowEpisodes");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "TvPlaylists");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "TvItem");

            migrationBuilder.DropColumn(
                name: "TimePlayed",
                table: "TvItem");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "SoundItems");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "SongPlaylists");

            migrationBuilder.AlterColumn<string>(
                name: "TimePlayed",
                table: "VideoItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "TimePlayed",
                table: "SoundItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "TEXT");
        }
    }
}
