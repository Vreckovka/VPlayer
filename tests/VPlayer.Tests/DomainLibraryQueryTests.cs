using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using Logger;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Providers;
using VCore.WPF.Modularity.Events;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Core.ViewModels.TvShows;
using VPlayer.Home.ViewModels;
using VPlayer.Home.ViewModels.Albums;
using VPlayer.Home.ViewModels.Artists;
using VPlayer.Home.ViewModels.LibraryViewModels;
using VPlayer.Home.ViewModels.TvShows;
using Xunit;

namespace VPlayer.Tests
{
  public class DomainLibraryQueryTests
  {
    private sealed class Context : AudioDatabaseContext
    {
      private readonly SqliteConnection connection;
      public Context(SqliteConnection connection) => this.connection=connection;
      protected override void OnConfiguring(DbContextOptionsBuilder builder) => builder.UseSqlite(connection);
    }
    private sealed class Fixture : IDisposable
    {
      private readonly SqliteConnection connection=new SqliteConnection("Data Source=:memory:");
      public Context Database {get;}
      public Mock<IStorageManager> Storage {get;}=new Mock<IStorageManager>();
      public IViewModelsFactory Factory {get;}=new Mock<IViewModelsFactory>().Object;
      public ILogger Logger {get;}=new Mock<ILogger>().Object;
      public IRegionProvider Regions {get;}=new Mock<IRegionProvider>().Object;
      public EventAggregator Events {get;}=new EventAggregator();
      public Fixture()
      {
        connection.Open();
        Database=new Context(connection);
        Database.Database.EnsureCreated();
        Storage.Setup(x=>x.SubscribeToItemChange<Album>(It.IsAny<Action<IItemChanged<Album>>>())).Returns(Disposable.Empty);
        Storage.Setup(x=>x.SubscribeToItemChange<Artist>(It.IsAny<Action<IItemChanged<Artist>>>())).Returns(Disposable.Empty);
      }
      public LibraryCollection<TView,TModel> Library<TView,TModel>()
        where TView : class,INamedEntityViewModel<TModel>
        where TModel : class,INamedEntity
      {
        Storage.Setup(x=>x.GetTempRepository<TModel>()).Returns(Database.Set<TModel>().AsNoTracking());
        return new LibraryCollection<TView,TModel>(Factory,Storage.Object,Logger);
      }
      public void Dispose() {Database.Dispose();connection.Dispose();}
    }

    [Fact]
    public void ArtistAndAlbumQueriesRetainNestedRelationshipsAfterReset()
    {
      using var fixture=new Fixture();
      var artist=new Artist("Artist");
      var album=new Album {Name="Album",Artist=artist};
      album.Songs.Add(new Song {ItemModel=new SoundItem {FileInfoEntity=new FileInfoEntity {Title="Song"}}});
      fixture.Database.Albums.Add(album);
      fixture.Database.SaveChanges();
      var artists=fixture.Library<ArtistViewModel,Artist>();
      var albums=fixture.Library<AlbumViewModel,Album>();
      using var artistsView=new ArtistsViewModel(fixture.Regions,fixture.Factory,fixture.Storage.Object,artists,fixture.Events);
      using var albumsView=new AlbumsViewModel(fixture.Regions,fixture.Factory,fixture.Storage.Object,albums,fixture.Events,fixture.Logger);
      for(int i=0;i<2;i++)
      {
        var loadedArtist=artistsView.LoadQuery.Single();
        Assert.Single(loadedArtist.Albums);
        Assert.Single(loadedArtist.Albums.Single().Songs);
        var loadedAlbum=albumsView.LoadQuery.Single();
        Assert.Equal(artist.Id,loadedAlbum.Artist.Id);
        Assert.Single(loadedAlbum.Songs);
        Assert.NotSame(artist,loadedArtist);
        artists.Clear();
        albums.Clear();
      }
    }

    [Fact]
    public void TvShowQueryRetainsSortedSeasonsEpisodesAndVideoRelationships()
    {
      using var fixture=new Fixture();
      var show=new TvShow
      {
        Name="Series",
        Seasons=new[] {3,1,2}.Select(season=>new TvShowSeason
        {
          SeasonNumber=season,
          Episodes=new[] {5,1,3}.Select(episode=>new TvShowEpisode
          {
            EpisodeNumber=episode,
            VideoItem=new VideoItem {FileInfoEntity=new FileInfoEntity {Title="Episode"}}
          }).ToList()
        }).ToList()
      };
      fixture.Database.TvShows.Add(show);
      fixture.Database.SaveChanges();
      var library=fixture.Library<TvShowViewModel,TvShow>();
      using var view=new TvShowsViewModel(fixture.Regions,fixture.Factory,fixture.Storage.Object,library,fixture.Events,fixture.Logger);
      var loaded=view.LoadQuery.Single();
      Assert.Equal(new[] {1,2,3},loaded.Seasons.Select(x=>x.SeasonNumber));
      foreach(var season in loaded.Seasons)
      {
        Assert.Equal(new[] {1,3,5},season.Episodes.Select(x=>x.EpisodeNumber));
        Assert.All(season.Episodes,x=>Assert.NotNull(x.VideoItem));
      }
    }

    [Fact]
    public void PlaylistQueryKeepsPrivacyFilterAndLastPlayedOrderingAfterReset()
    {
      using var fixture=new Fixture();
      fixture.Database.SoundItemPlaylists.AddRange(
        new SoundItemFilePlaylist {Name="Older",LastPlayed=new DateTime(2020,1,1)},
        new SoundItemFilePlaylist {Name="Newer",LastPlayed=new DateTime(2020,1,2)},
        new SoundItemFilePlaylist {Name="Private",LastPlayed=new DateTime(2020,1,3),IsPrivate=true});
      fixture.Database.SaveChanges();
      var library=fixture.Library<SongsPlaylistViewModel,SoundItemFilePlaylist>();
      using var view=new SoundItemPlaylistsViewModel(fixture.Regions,fixture.Factory,fixture.Storage.Object,
        new Mock<ISettingsProvider>().Object,library,fixture.Events);
      for(int i=0;i<2;i++)
      {
        Assert.Equal(new[] {"Newer","Older"},view.LoadQuery.Select(x=>x.Name).ToArray());
        library.Clear();
      }
    }
  }
}