using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class initlize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Artists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlbumIdCover = table.Column<int>(type: "INTEGER", nullable: true),
                    ArtistCover = table.Column<string>(type: "TEXT", nullable: true),
                    MusicBrainzId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    InfoDownloadStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SongPlaylists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    HashCode = table.Column<long>(type: "INTEGER", nullable: true),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: true),
                    IsReapting = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsShuffle = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastItemElapsedTime = table.Column<float>(type: "REAL", nullable: false),
                    LastItemIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    IsUserCreated = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastPlayed = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongPlaylists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TvShowPlaylists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    HashCode = table.Column<long>(type: "INTEGER", nullable: true),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: true),
                    IsReapting = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsShuffle = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastItemElapsedTime = table.Column<float>(type: "REAL", nullable: false),
                    LastItemIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    IsUserCreated = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastPlayed = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvShowPlaylists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TvShows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    InfoDownloadStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvShows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Albums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlbumFrontCoverFilePath = table.Column<string>(type: "TEXT", nullable: true),
                    AlbumFrontCoverURI = table.Column<string>(type: "TEXT", nullable: true),
                    ArtistId = table.Column<int>(type: "INTEGER", nullable: true),
                    MusicBrainzId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    ReleaseDate = table.Column<string>(type: "TEXT", nullable: true),
                    InfoDownloadStatus = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Albums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Albums_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TvShowEpisodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiskLocation = table.Column<string>(type: "TEXT", nullable: true),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    Length = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    InfoDownloadStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    EpisodeNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    SeasonNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TvShowId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvShowEpisodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TvShowEpisodes_TvShows_TvShowId",
                        column: x => x.TvShowId,
                        principalTable: "TvShows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Songs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlbumId = table.Column<int>(type: "INTEGER", nullable: true),
                    DiskLocation = table.Column<string>(type: "TEXT", nullable: true),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    Length = table.Column<int>(type: "INTEGER", nullable: false),
                    MusicBrainzId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Chartlyrics_Lyric = table.Column<string>(type: "TEXT", nullable: true),
                    Chartlyrics_LyricId = table.Column<string>(type: "TEXT", nullable: true),
                    Chartlyrics_LyricCheckSum = table.Column<string>(type: "TEXT", nullable: true),
                    LRCLyrics = table.Column<string>(type: "TEXT", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Songs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Songs_Albums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlaylistsTvShowEpisode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderInPlaylist = table.Column<int>(type: "INTEGER", nullable: false),
                    IdTvShowEpisode = table.Column<int>(type: "INTEGER", nullable: false),
                    TvShowPlaylistId = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistsTvShowEpisode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaylistsTvShowEpisode_TvShowEpisodes_IdTvShowEpisode",
                        column: x => x.IdTvShowEpisode,
                        principalTable: "TvShowEpisodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlaylistsTvShowEpisode_TvShowPlaylists_TvShowPlaylistId",
                        column: x => x.TvShowPlaylistId,
                        principalTable: "TvShowPlaylists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlaylistSongs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderInPlaylist = table.Column<int>(type: "INTEGER", nullable: false),
                    IdSong = table.Column<int>(type: "INTEGER", nullable: false),
                    SongsPlaylistId = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistSongs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaylistSongs_SongPlaylists_SongsPlaylistId",
                        column: x => x.SongsPlaylistId,
                        principalTable: "SongPlaylists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlaylistSongs_Songs_IdSong",
                        column: x => x.IdSong,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Albums_ArtistId",
                table: "Albums",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistSongs_IdSong",
                table: "PlaylistSongs",
                column: "IdSong");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistSongs_SongsPlaylistId",
                table: "PlaylistSongs",
                column: "SongsPlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistsTvShowEpisode_IdTvShowEpisode",
                table: "PlaylistsTvShowEpisode",
                column: "IdTvShowEpisode");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistsTvShowEpisode_TvShowPlaylistId",
                table: "PlaylistsTvShowEpisode",
                column: "TvShowPlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_AlbumId",
                table: "Songs",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_TvShowEpisodes_TvShowId",
                table: "TvShowEpisodes",
                column: "TvShowId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaylistSongs");

            migrationBuilder.DropTable(
                name: "PlaylistsTvShowEpisode");

            migrationBuilder.DropTable(
                name: "SongPlaylists");

            migrationBuilder.DropTable(
                name: "Songs");

            migrationBuilder.DropTable(
                name: "TvShowEpisodes");

            migrationBuilder.DropTable(
                name: "TvShowPlaylists");

            migrationBuilder.DropTable(
                name: "Albums");

            migrationBuilder.DropTable(
                name: "TvShows");

            migrationBuilder.DropTable(
                name: "Artists");
        }
    }
}
