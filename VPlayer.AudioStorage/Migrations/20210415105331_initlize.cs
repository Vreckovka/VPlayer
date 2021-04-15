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
                    TotalPlayedTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    IsUserCreated = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastPlayed = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongPlaylists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TvChannelGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvChannelGroups", x => x.Id);
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
                    TotalPlayedTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
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
                    CsfdUrl = table.Column<string>(type: "TEXT", nullable: true),
                    PosterPath = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvShows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TvSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    TvSourceType = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceConnection = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UPnPDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceTypeText = table.Column<string>(type: "TEXT", nullable: true),
                    UDN = table.Column<string>(type: "TEXT", nullable: true),
                    FriendlyName = table.Column<string>(type: "TEXT", nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    ManufacturerURL = table.Column<string>(type: "TEXT", nullable: true),
                    ModelName = table.Column<string>(type: "TEXT", nullable: true),
                    ModelURL = table.Column<string>(type: "TEXT", nullable: true),
                    ModelDescription = table.Column<string>(type: "TEXT", nullable: true),
                    ModelNumber = table.Column<string>(type: "TEXT", nullable: true),
                    SerialNumber = table.Column<string>(type: "TEXT", nullable: true),
                    UPC = table.Column<string>(type: "TEXT", nullable: true),
                    PresentationURL = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UPnPDevices", x => x.Id);
                });

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
                    CropRatio = table.Column<string>(type: "TEXT", nullable: true),
                    AudioTrack = table.Column<int>(type: "INTEGER", nullable: true),
                    SubtitleTrack = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoItems", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "TvChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    IdTvSource = table.Column<int>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TvChannels_TvSources_IdTvSource",
                        column: x => x.IdTvSource,
                        principalTable: "TvSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UPnPMediaServers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AliasURL = table.Column<string>(type: "TEXT", nullable: true),
                    PresentationURL = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultIconUrl = table.Column<string>(type: "TEXT", nullable: true),
                    OnlineServer = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContentDirectoryControlUrl = table.Column<string>(type: "TEXT", nullable: true),
                    UPnPDeviceId = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UPnPMediaServers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UPnPMediaServers_UPnPDevices_UPnPDeviceId",
                        column: x => x.UPnPDeviceId,
                        principalTable: "UPnPDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UPnPServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceType = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceId = table.Column<string>(type: "TEXT", nullable: true),
                    SCPDURL = table.Column<string>(type: "TEXT", nullable: true),
                    EventSubURL = table.Column<string>(type: "TEXT", nullable: true),
                    ControlURL = table.Column<string>(type: "TEXT", nullable: true),
                    UPnPDeviceId = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UPnPServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UPnPServices_UPnPDevices_UPnPDeviceId",
                        column: x => x.UPnPDeviceId,
                        principalTable: "UPnPDevices",
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
                    IdVideoItem = table.Column<int>(type: "INTEGER", nullable: false),
                    VideoPlaylistId = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistsTvShowEpisode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaylistsTvShowEpisode_TvShowPlaylists_VideoPlaylistId",
                        column: x => x.VideoPlaylistId,
                        principalTable: "TvShowPlaylists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlaylistsTvShowEpisode_VideoItems_IdVideoItem",
                        column: x => x.IdVideoItem,
                        principalTable: "VideoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "TvShowEpisodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InfoDownloadStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    TvShowId = table.Column<int>(type: "INTEGER", nullable: true),
                    TvShowSeasonId = table.Column<int>(type: "INTEGER", nullable: true),
                    VideoItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    EpisodeNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    DiskLocation = table.Column<string>(type: "TEXT", nullable: true),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    Length = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_TvShowEpisodes_TvShowsSeasons_TvShowSeasonId",
                        column: x => x.TvShowSeasonId,
                        principalTable: "TvShowsSeasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TvShowEpisodes_VideoItems_VideoItemId",
                        column: x => x.VideoItemId,
                        principalTable: "VideoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TvChannelGroupItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdTvChannel = table.Column<int>(type: "INTEGER", nullable: false),
                    TvChannelGroupId = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvChannelGroupItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TvChannelGroupItems_TvChannelGroups_TvChannelGroupId",
                        column: x => x.TvChannelGroupId,
                        principalTable: "TvChannelGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TvChannelGroupItems_TvChannels_IdTvChannel",
                        column: x => x.IdTvChannel,
                        principalTable: "TvChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_PlaylistsTvShowEpisode_IdVideoItem",
                table: "PlaylistsTvShowEpisode",
                column: "IdVideoItem");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistsTvShowEpisode_VideoPlaylistId",
                table: "PlaylistsTvShowEpisode",
                column: "VideoPlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_AlbumId",
                table: "Songs",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_TvChannelGroupItems_IdTvChannel",
                table: "TvChannelGroupItems",
                column: "IdTvChannel");

            migrationBuilder.CreateIndex(
                name: "IX_TvChannelGroupItems_TvChannelGroupId",
                table: "TvChannelGroupItems",
                column: "TvChannelGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TvChannels_IdTvSource",
                table: "TvChannels",
                column: "IdTvSource");

            migrationBuilder.CreateIndex(
                name: "IX_TvShowEpisodes_TvShowId",
                table: "TvShowEpisodes",
                column: "TvShowId");

            migrationBuilder.CreateIndex(
                name: "IX_TvShowEpisodes_TvShowSeasonId",
                table: "TvShowEpisodes",
                column: "TvShowSeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_TvShowEpisodes_VideoItemId",
                table: "TvShowEpisodes",
                column: "VideoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TvShowsSeasons_TvShowId",
                table: "TvShowsSeasons",
                column: "TvShowId");

            migrationBuilder.CreateIndex(
                name: "IX_UPnPMediaServers_UPnPDeviceId",
                table: "UPnPMediaServers",
                column: "UPnPDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_UPnPServices_UPnPDeviceId",
                table: "UPnPServices",
                column: "UPnPDeviceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaylistSongs");

            migrationBuilder.DropTable(
                name: "PlaylistsTvShowEpisode");

            migrationBuilder.DropTable(
                name: "TvChannelGroupItems");

            migrationBuilder.DropTable(
                name: "TvShowEpisodes");

            migrationBuilder.DropTable(
                name: "UPnPMediaServers");

            migrationBuilder.DropTable(
                name: "UPnPServices");

            migrationBuilder.DropTable(
                name: "SongPlaylists");

            migrationBuilder.DropTable(
                name: "Songs");

            migrationBuilder.DropTable(
                name: "TvShowPlaylists");

            migrationBuilder.DropTable(
                name: "TvChannelGroups");

            migrationBuilder.DropTable(
                name: "TvChannels");

            migrationBuilder.DropTable(
                name: "TvShowsSeasons");

            migrationBuilder.DropTable(
                name: "VideoItems");

            migrationBuilder.DropTable(
                name: "UPnPDevices");

            migrationBuilder.DropTable(
                name: "Albums");

            migrationBuilder.DropTable(
                name: "TvSources");

            migrationBuilder.DropTable(
                name: "TvShows");

            migrationBuilder.DropTable(
                name: "Artists");
        }
    }
}
