using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Reflection;
using VPLayer.Domain;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
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
  [Collection("UI synchronization")]
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

    private static Task UntilAsync(Func<bool> condition) => UntilAsync(()=>Task.FromResult(condition()));
    private static async Task UntilAsync(Func<Task<bool>> condition)
    {
      for(int i=0;i<500;i++)
      {
        if(await condition()) return;
        await Task.Delay(10);
      }
      throw new TimeoutException("Cached relationship notification did not arrive.");
    }
    [Fact]
    public void CachedRelationshipsObserveEditsAndNestedChangesWithoutLoadingLibraries()
    {
      LibraryLoadingTests.WithDispatcher(async () =>
      {
        using var fixture=new Fixture();
        fixture.Database.Artists.AddRange(Enumerable.Range(1,10000).Select(i=>new Artist {Name="Artist "+i}));
        fixture.Database.SaveChanges();
        var storedArtist=fixture.Database.Artists.OrderBy(x=>x.Id).Last();
        var storedAlbum=new Album {Name="Album",Artist=storedArtist};
        fixture.Database.Albums.Add(storedAlbum);
        fixture.Database.SaveChanges();
        var contexts=new ConcurrentBag<Context>();
        try
        {
          fixture.Storage.Setup(x=>x.GetTempRepository<Artist>()).Returns(()=>
          {
            var context=new Context((SqliteConnection)fixture.Database.Database.GetDbConnection());
            contexts.Add(context);
            return context.Artists.AsNoTracking();
          });
          fixture.Storage.Setup(x=>x.GetTempRepository<Album>()).Returns(()=>
          {
            var context=new Context((SqliteConnection)fixture.Database.Database.GetDbConnection());
            contexts.Add(context);
            return context.Albums.AsNoTracking();
          });
          var artistObservers=new List<Action<IItemChanged<Artist>>>();
          var albumObservers=new List<Action<IItemChanged<Album>>>();
          fixture.Storage.Setup(x=>x.SubscribeToItemChange<Artist>(It.IsAny<Action<IItemChanged<Artist>>>()))
            .Callback<Action<IItemChanged<Artist>>>(artistObservers.Add).Returns(Disposable.Empty);
          fixture.Storage.Setup(x=>x.SubscribeToItemChange<Album>(It.IsAny<Action<IItemChanged<Album>>>()))
            .Callback<Action<IItemChanged<Album>>>(albumObservers.Add).Returns(Disposable.Empty);
          var factory=new Mock<IViewModelsFactory>();
          var artistLibrary=new LibraryCollection<ArtistViewModel,Artist>(factory.Object,fixture.Storage.Object,fixture.Logger);
          var albumLibrary=new LibraryCollection<AlbumViewModel,Album>(factory.Object,fixture.Storage.Object,fixture.Logger);
          using var artists=new ArtistsViewModel(fixture.Regions,factory.Object,fixture.Storage.Object,artistLibrary,fixture.Events);
          using var albums=new AlbumsViewModel(fixture.Regions,factory.Object,fixture.Storage.Object,albumLibrary,fixture.Events,fixture.Logger);
          var cloud=new Mock<IVPlayerCloudService>().Object;
          var regions=new Mock<IVPlayerRegionProvider>().Object;
          factory.Setup(x=>x.Create<ArtistViewModel>(It.IsAny<object[]>()))
            .Returns((object[] args)=>new ArtistViewModel((Artist)args[0],fixture.Events,fixture.Storage.Object,cloud,artists,factory.Object,regions));
          factory.Setup(x=>x.Create<AlbumViewModel>(It.IsAny<object[]>()))
            .Returns((object[] args)=>new AlbumViewModel((Album)args[0],fixture.Events,fixture.Storage.Object,albums,factory.Object,cloud,regions));
          await artists.PrepareViewModelsAsync(Enumerable.Repeat(storedArtist.Id,100000));
          await albums.PrepareViewModelsAsync(Enumerable.Repeat(storedAlbum.Id,100000));
          using var artist=await artists.GetViewModelAsync(storedArtist.Id);
          Assert.Equal(storedAlbum.Id,Assert.Single(artist.Model.Albums).Id);
          using var album=await albums.GetViewModelAsync(storedAlbum.Id);
          artist.IsInPlaylist=album.IsInPlaylist=true;
          storedArtist.Name="Renamed Artist";
          fixture.Database.SaveChanges();
          foreach(var observer in artistObservers)
          {
            var change=new Mock<IItemChanged<Artist>>();
            change.SetupGet(x=>x.Item).Returns(storedArtist);
            change.SetupGet(x=>x.Changed).Returns(Changed.Updated);
            observer(change.Object);


          }
          await UntilAsync(()=>artist.Name==storedArtist.Name && album.Model.Artist.Name==storedArtist.Name);
          Assert.Equal(storedArtist.Name,artist.Name);
          Assert.Equal(storedArtist.Name,album.Model.Artist.Name);
          var addedAlbum=new Album {Name="Added Album",Artist=storedArtist};
          fixture.Database.Albums.Add(addedAlbum);
          fixture.Database.SaveChanges();
          foreach(var observer in albumObservers)
          {
            var change=new Mock<IItemChanged<Album>>();
            change.SetupGet(x=>x.Item).Returns(addedAlbum);
            change.SetupGet(x=>x.Changed).Returns(Changed.Added);
            observer(change.Object);

          }
          await UntilAsync(()=>artist.Model.Albums.Count==2);
          Assert.Equal(2,artist.Model.Albums.Count);
          var song=new Song {Album=storedAlbum,ItemModel=new SoundItem {FileInfoEntity=new FileInfoEntity {Title="Added Song"}}};
          fixture.Database.Songs.Add(song);
          fixture.Database.SaveChanges();
          var songChange=new Mock<IItemChanged<Song>>();
          songChange.SetupGet(x=>x.Item).Returns(song);
          songChange.SetupGet(x=>x.Changed).Returns(Changed.Added);
          typeof(AlbumsViewModel).GetMethod("SongChange",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(albums,new object[] {songChange.Object});
          await UntilAsync(()=>album.Model.Songs.Count==1);
          Assert.Same(album,await albums.GetViewModelAsync(storedAlbum.Id));
          Assert.Single(album.Model.Songs);
          Assert.True(artist.IsInPlaylist && album.IsInPlaylist);
          Assert.False(artistLibrary.WasLoaded);
          Assert.False(albumLibrary.WasLoaded);
          factory.Verify(x=>x.Create<ArtistViewModel>(It.IsAny<object[]>()),Times.Once);
          factory.Verify(x=>x.Create<AlbumViewModel>(It.IsAny<object[]>()),Times.Once);
          fixture.Database.Artists.Remove(storedArtist);
          fixture.Database.SaveChanges();
          foreach(var observer in artistObservers)
          {
            var change=new Mock<IItemChanged<Artist>>();
            change.SetupGet(x=>x.Item).Returns(storedArtist);
            change.SetupGet(x=>x.Changed).Returns(Changed.Removed);
            observer(change.Object);


          }
          await UntilAsync(async()=>await artists.GetViewModelAsync(storedArtist.Id)==null && await albums.GetViewModelAsync(storedAlbum.Id)==null);
          Assert.Null(await artists.GetViewModelAsync(storedArtist.Id));
          Assert.Null(await albums.GetViewModelAsync(storedAlbum.Id));
        }
        finally {foreach(var context in contexts) context.Dispose();}
      });
    }
    [Fact]
    public void RelationshipSnapshotRefreshPreservesPlaybackStateAndNestedData()
    {
      using var fixture=new Fixture();
      var factory=new Mock<IViewModelsFactory>().Object;
      var cloud=new Mock<IVPlayerCloudService>().Object;
      var regions=new Mock<IVPlayerRegionProvider>().Object;
      using var artist=new ArtistViewModel(new Artist {Id=1,ArtistCover="old"},fixture.Events,fixture.Storage.Object,
        cloud,new Mock<IArtistsViewModel>().Object,factory,regions);
      using var album=new AlbumViewModel(new Album {Id=2,Name="Old"},fixture.Events,fixture.Storage.Object,
        new Mock<IAlbumsViewModel>().Object,factory,cloud,regions);
      artist.IsPlaying=artist.IsInPlaylist=true;
      album.IsPlaying=album.IsInPlaylist=true;
      var freshArtist=new Artist
      {
        Id=1,Name="Fresh Artist",ArtistCover="new",
        Albums=Enumerable.Range(1,10000).Select(i=>new Album {Id=i,Name="Album "+i}).ToList()
      };
      var freshAlbum=new Album {Id=2,Name="Fresh Album",Artist=freshArtist,ArtistId=1,
        Songs=Enumerable.Range(1,10000).Select(i=>new Song {Id=i}).ToList()};
      artist.RefreshModel(freshArtist);
      album.RefreshModel(freshAlbum);
      Assert.Same(freshArtist,artist.Model);
      Assert.Equal(freshArtist.Name,artist.Name);
      Assert.Equal("new",artist.ImageThumbnail);
      Assert.Equal("10000 albums",artist.BottomText);
      Assert.Same(freshAlbum,album.Model);
      Assert.Same(freshArtist,album.Model.Artist);
      Assert.Equal(10000,album.Model.Songs.Count);
      Assert.True(artist.IsPlaying && artist.IsInPlaylist);
      Assert.True(album.IsPlaying && album.IsInPlaylist);
      Assert.Throws<ArgumentException>(()=>artist.RefreshModel(new Artist {Id=3}));
      Assert.Same(freshArtist,artist.Model);
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