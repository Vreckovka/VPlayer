using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VPLayer.Domain;
using Logger;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Providers;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Home.ViewModels;
using VPlayer.Home.ViewModels.LibraryViewModels;
using Xunit;

namespace VPlayer.Tests
{
  public class PlaylistLoadingTests
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
      public Mock<IViewModelsFactory> Factory {get;}=new Mock<IViewModelsFactory>();
      private readonly EventAggregator events=new EventAggregator();
      public Fixture()
      {
        connection.Open();
        Database=new Context(connection);
        Database.Database.EnsureCreated();
        Storage.Setup(x=>x.GetTempRepository<SoundItemFilePlaylist>()).Returns(Database.SoundItemPlaylists.AsNoTracking());
        Storage.Setup(x=>x.GetTempRepository<PlaylistSoundItem>()).Returns(Database.PlaylistSongs.AsNoTracking());
        Factory.Setup(x=>x.Create<SoundItemInPlaylistViewModel>(It.IsAny<object[]>()))
          .Returns((object[] args)=>new SoundItemInPlaylistViewModel((SoundItem)args[0],events,Storage.Object));
      }
      public SongsPlaylistViewModel Create(int id)
      {
        var logger=new Mock<ILogger>().Object;
        var collection=new LibraryCollection<SongsPlaylistViewModel,SoundItemFilePlaylist>(Factory.Object,Storage.Object,logger);
        var owner=new SoundItemPlaylistsViewModel(new Mock<IRegionProvider>().Object,Factory.Object,Storage.Object,
          new Mock<ISettingsProvider>().Object,collection,events);
        return new SongsPlaylistViewModel(new SoundItemFilePlaylist {Id=id,Name="stale metadata"},events,Factory.Object,owner,
          new Mock<IVPlayerCloudService>().Object,Storage.Object,logger,new Mock<IWindowManager>().Object);
      }
      public void Dispose() {Database.Dispose();connection.Dispose();}
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10000)]
    public async Task LoadsFreshMetadataAndEveryOccurrenceInOrder(int size)
    {
      using var fixture=new Fixture();
      var first=new SoundItem {FileInfoEntity=new FileInfoEntity {Title="First"},Duration=11};
      var second=new SoundItem {FileInfoEntity=new FileInfoEntity {Title="Second"},Duration=22};
      var playlist=new SoundItemFilePlaylist
      {
        Name="Fresh metadata",IsShuffle=true,ItemCount=size,
        PlaylistItems=Enumerable.Range(0,size).Reverse().Select(i=>new PlaylistSoundItem
        {
          ReferencedItem=i%2==0?first:second,OrderInPlaylist=i/2
        }).ToList()
      };
      fixture.Database.SoundItemPlaylists.Add(playlist);
      fixture.Database.SoundItemPlaylists.Add(new SoundItemFilePlaylist
      {
        Name="Unrelated playlist",PlaylistItems=new List<PlaylistSoundItem>
        {new PlaylistSoundItem {ReferencedItem=first,OrderInPlaylist=-1}}
      });
      fixture.Database.SaveChanges();
      var expected=playlist.PlaylistItems.OrderBy(x=>x.OrderInPlaylist).ThenBy(x=>x.Id).ToArray();
      using var viewModel=fixture.Create(playlist.Id);
      var loaded=(await viewModel.GetItemsToPlay()).ToArray();
      try
      {
        Assert.Equal(size,loaded.Length);
        Assert.Equal("Fresh metadata",viewModel.Model.Name);
        Assert.True(viewModel.Model.IsShuffle);
        Assert.Equal(expected.Select(x=>x.Id),viewModel.Model.PlaylistItems.OrderBy(x=>x.OrderInPlaylist).ThenBy(x=>x.Id).Select(x=>x.Id));
        Assert.Equal(expected.Select(x=>x.IdReferencedItem),loaded.Select(x=>x.Model.Id));
        Assert.Equal(expected.Select(x=>x.ReferencedItem.Name),loaded.Select(x=>x.Name));
        Assert.Equal(size,loaded.Distinct().Count());
        Assert.All(loaded,x=>Assert.NotNull(x.Model.FileInfoEntity));
        fixture.Factory.Verify(x=>x.Create<SoundItemInPlaylistViewModel>(It.IsAny<object[]>()),Times.Exactly(size));
      }
      finally {foreach(var view in loaded) view.Dispose();}
    }

    [Fact]
    public async Task DeletedPlaylistReturnsNullWithoutCreatingViews()
    {
      using var fixture=new Fixture();
      using var viewModel=fixture.Create(int.MaxValue);
      Assert.Null(await viewModel.GetItemsToPlay());
      fixture.Factory.Verify(x=>x.Create<SoundItemInPlaylistViewModel>(It.IsAny<object[]>()),Times.Never());
    }

    [Fact]
    public async Task TrackWithoutOptionalFileMetadataRemainsInPlaylist()
    {
      using var fixture=new Fixture();
      var playlist=new SoundItemFilePlaylist
      {
        Name="Missing metadata",PlaylistItems=new List<PlaylistSoundItem>
        {new PlaylistSoundItem {ReferencedItem=new SoundItem(),OrderInPlaylist=0}}
      };
      fixture.Database.SoundItemPlaylists.Add(playlist);
      fixture.Database.SaveChanges();
      using var viewModel=fixture.Create(playlist.Id);
      var loaded=(await viewModel.GetItemsToPlay()).ToArray();
      try
      {
        Assert.Single(loaded);
        Assert.Equal(playlist.PlaylistItems[0].IdReferencedItem,loaded[0].Model.Id);
        Assert.Null(loaded[0].Model.FileInfoEntity);
      }
      finally {foreach(var view in loaded) view.Dispose();}
    }
  }
}