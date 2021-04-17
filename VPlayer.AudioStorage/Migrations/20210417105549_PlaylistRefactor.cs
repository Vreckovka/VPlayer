using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class PlaylistRefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistSongs_Songs_IdSong",
                table: "PlaylistSongs");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistsTvShowEpisode_VideoItems_IdVideoItem",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.DropForeignKey(
                name: "FK_TvPlaylistItem_TvChannelGroups_IdTvChannelGroup",
                table: "TvPlaylistItem");

            migrationBuilder.DropIndex(
                name: "IX_TvPlaylistItem_IdTvChannelGroup",
                table: "TvPlaylistItem");

            migrationBuilder.DropColumn(
                name: "IdTvChannelGroup",
                table: "TvPlaylistItem");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "TvPlaylistItem");

            migrationBuilder.RenameColumn(
                name: "IsFavorite",
                table: "TvPlaylistItem",
                newName: "IdReferencedItem");

            migrationBuilder.RenameColumn(
                name: "IdVideoItem",
                table: "PlaylistsTvShowEpisode",
                newName: "IdReferencedItem");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistsTvShowEpisode_IdVideoItem",
                table: "PlaylistsTvShowEpisode",
                newName: "IX_PlaylistsTvShowEpisode_IdReferencedItem");

            migrationBuilder.RenameColumn(
                name: "IdSong",
                table: "PlaylistSongs",
                newName: "IdReferencedItem");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistSongs_IdSong",
                table: "PlaylistSongs",
                newName: "IX_PlaylistSongs_IdReferencedItem");

            migrationBuilder.AddColumn<int>(
                name: "IdTvItem",
                table: "TvChannels",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TvItemId",
                table: "TvChannels",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdTvItem",
                table: "TvChannelGroups",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TvItemId",
                table: "TvChannelGroups",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TvItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvItem", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TvPlaylistItem_IdReferencedItem",
                table: "TvPlaylistItem",
                column: "IdReferencedItem");

            migrationBuilder.CreateIndex(
                name: "IX_TvChannels_TvItemId",
                table: "TvChannels",
                column: "TvItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TvChannelGroups_TvItemId",
                table: "TvChannelGroups",
                column: "TvItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistSongs_Songs_IdReferencedItem",
                table: "PlaylistSongs",
                column: "IdReferencedItem",
                principalTable: "Songs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistsTvShowEpisode_VideoItems_IdReferencedItem",
                table: "PlaylistsTvShowEpisode",
                column: "IdReferencedItem",
                principalTable: "VideoItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TvChannelGroups_TvItem_TvItemId",
                table: "TvChannelGroups",
                column: "TvItemId",
                principalTable: "TvItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TvChannels_TvItem_TvItemId",
                table: "TvChannels",
                column: "TvItemId",
                principalTable: "TvItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TvPlaylistItem_TvItem_IdReferencedItem",
                table: "TvPlaylistItem",
                column: "IdReferencedItem",
                principalTable: "TvItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistSongs_Songs_IdReferencedItem",
                table: "PlaylistSongs");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistsTvShowEpisode_VideoItems_IdReferencedItem",
                table: "PlaylistsTvShowEpisode");

            migrationBuilder.DropForeignKey(
                name: "FK_TvChannelGroups_TvItem_TvItemId",
                table: "TvChannelGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_TvChannels_TvItem_TvItemId",
                table: "TvChannels");

            migrationBuilder.DropForeignKey(
                name: "FK_TvPlaylistItem_TvItem_IdReferencedItem",
                table: "TvPlaylistItem");

            migrationBuilder.DropTable(
                name: "TvItem");

            migrationBuilder.DropIndex(
                name: "IX_TvPlaylistItem_IdReferencedItem",
                table: "TvPlaylistItem");

            migrationBuilder.DropIndex(
                name: "IX_TvChannels_TvItemId",
                table: "TvChannels");

            migrationBuilder.DropIndex(
                name: "IX_TvChannelGroups_TvItemId",
                table: "TvChannelGroups");

            migrationBuilder.DropColumn(
                name: "IdTvItem",
                table: "TvChannels");

            migrationBuilder.DropColumn(
                name: "TvItemId",
                table: "TvChannels");

            migrationBuilder.DropColumn(
                name: "IdTvItem",
                table: "TvChannelGroups");

            migrationBuilder.DropColumn(
                name: "TvItemId",
                table: "TvChannelGroups");

            migrationBuilder.RenameColumn(
                name: "IdReferencedItem",
                table: "TvPlaylistItem",
                newName: "IsFavorite");

            migrationBuilder.RenameColumn(
                name: "IdReferencedItem",
                table: "PlaylistsTvShowEpisode",
                newName: "IdVideoItem");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistsTvShowEpisode_IdReferencedItem",
                table: "PlaylistsTvShowEpisode",
                newName: "IX_PlaylistsTvShowEpisode_IdVideoItem");

            migrationBuilder.RenameColumn(
                name: "IdReferencedItem",
                table: "PlaylistSongs",
                newName: "IdSong");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistSongs_IdReferencedItem",
                table: "PlaylistSongs",
                newName: "IX_PlaylistSongs_IdSong");

            migrationBuilder.AddColumn<int>(
                name: "IdTvChannelGroup",
                table: "TvPlaylistItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "TvPlaylistItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TvPlaylistItem_IdTvChannelGroup",
                table: "TvPlaylistItem",
                column: "IdTvChannelGroup");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistSongs_Songs_IdSong",
                table: "PlaylistSongs",
                column: "IdSong",
                principalTable: "Songs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistsTvShowEpisode_VideoItems_IdVideoItem",
                table: "PlaylistsTvShowEpisode",
                column: "IdVideoItem",
                principalTable: "VideoItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TvPlaylistItem_TvChannelGroups_IdTvChannelGroup",
                table: "TvPlaylistItem",
                column: "IdTvChannelGroup",
                principalTable: "TvChannelGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
