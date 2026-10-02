using System;
using System.Collections.Generic;
using System.Linq;
using ChromeDriverScrapper;
using Logger;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Clients.MusixMatch;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud.Images;
using VPlayer.Core.Managers.Status;
using Xunit;

namespace VPlayer.Tests
{
  public class PlaylistPersistenceTests
  {
    private sealed class Context : AudioDatabaseContext
    {
      private readonly SqliteConnection connection;
      private readonly Action<Context> disposed;
      private readonly Action saving;
      internal Context(SqliteConnection connection,Action<Context> disposed=null,Action saving=null){this.connection=connection;this.disposed=disposed;this.saving=saving;}
      protected override void OnConfiguring(DbContextOptionsBuilder builder)=>builder.UseSqlite(connection);
      public override int SaveChanges(){saving?.Invoke();return base.SaveChanges();}
      public override void Dispose(){try{disposed?.Invoke(this);}finally{base.Dispose();}}
    }
    private sealed class Storage : VPlayerStorageManager
    {
      internal Func<AudioDatabaseContext> ContextFactory;
      internal Storage():base(new AudioInfoDownloader(new Mock<ILogger>().Object,new Mock<IStatusManager>().Object,
        new Mock<IPCloudAlbumCoverProvider>().Object,new MusixMatchLyricsProvider(new Mock<IChromeDriverProvider>().Object,
        new Mock<ILogger>().Object)),new Mock<ILogger>().Object){}
      protected override AudioDatabaseContext CreateEntityUpdateContext()=>ContextFactory();
      protected override IQueryable<T> GetPlaylistUpdateReadRepository<T>(AudioDatabaseContext context)=>context.Set<T>().AsNoTracking();
    }
    private sealed class Fixture : IDisposable
    {
      private readonly SqliteConnection connection=new SqliteConnection("Data Source=:memory:");
      internal readonly Storage Storage=new Storage();
      internal int Disposals;
      internal bool DetectChangesRestored;
      internal int FailWriteNumber;
      internal int Writes;
      internal Fixture()
      {
        connection.Open();
        using(var context=new Context(connection))
        {
          context.Database.EnsureCreated();
          context.SoundItems.AddRange(new SoundItem {Id=1,Duration=11,FileInfoEntity=new FileInfoEntity {Title="First"}},
            new SoundItem {Id=2,Duration=22,FileInfoEntity=new FileInfoEntity {Title="Second"}});
          context.Set<SoundItemFilePlaylist>().Add(new SoundItemFilePlaylist {Id=1,Name="Original",HashCode=777,ItemCount=100000});
          context.SaveChanges();
          context.Database.ExecuteSqlRaw(@"WITH RECURSIVE entries(n) AS (SELECT 1 UNION ALL SELECT n+1 FROM entries WHERE n<100000)
            INSERT INTO PlaylistSongs(Id,Created,Modified,IdReferencedItem,OrderInPlaylist,SoundItemFilePlaylistId)
            SELECT n,'2020-01-01 00:00:00','2020-01-01 00:00:00',1+n%2,n,1 FROM entries");
          context.Database.ExecuteSqlRaw("UPDATE SoundItemPlaylists SET ActualItemId=100000 WHERE Id=1");
        }
        Storage.ContextFactory=()=>new Context(connection,ctx=>{Disposals++;DetectChangesRestored=ctx.ChangeTracker.AutoDetectChangesEnabled;},
          ()=>{Writes++;if(Writes==FailWriteNumber) throw new InvalidOperationException("Injected write failure");});
      }
      internal Context ReadContext()=>new Context(connection);
      internal SoundItemFilePlaylist Read()
      {
        using var context=ReadContext();
        return context.Set<SoundItemFilePlaylist>().AsNoTracking().AsSplitQuery()
          .Include(x=>x.PlaylistItems).ThenInclude(x=>x.ReferencedItem.FileInfoEntity)
          .Include(x=>x.ActualItem.ReferencedItem).Single(x=>x.Id==1);
      }
      internal SoundItemFilePlaylist Save(SoundItemFilePlaylist input)
      {
        Assert.True(Storage.UpdatePlaylist<SoundItemFilePlaylist,PlaylistSoundItem,SoundItem>(input,out var updated));
        Assert.NotNull(updated);
        return updated;
      }
      public void Dispose(){Storage.Dispose();connection.Dispose();}
    }

    [Fact]
    public void EmptyQueueRemovesEveryStoredOccurrenceAndClearsActualItem()
    {
      using var f=new Fixture();
      var input=new SoundItemFilePlaylist {Id=1,Name="Empty",HashCode=0,ItemCount=0,PlaylistItems=new List<PlaylistSoundItem>()};
      var updated=f.Save(input);
      Assert.Empty(updated.PlaylistItems);
      Assert.Null(updated.ActualItemId);
      using var context=f.ReadContext();
      Assert.Empty(context.PlaylistSongs);
      var stored=context.Set<SoundItemFilePlaylist>().Single(x=>x.Id==1);
      Assert.Equal(0,stored.ItemCount);
      Assert.Null(stored.ActualItemId);
      Assert.Equal("Empty",stored.Name);
      Assert.Equal(2,context.SoundItems.Count());
      Assert.Equal(1,f.Disposals);
      Assert.True(f.DetectChangesRestored);
    }

    [Fact]
    public void ReplacingHalfOfHugeQueueKeepsSurvivorsAndGeneratesActualOccurrenceIdentity()
    {
      using var f=new Fixture();
      var rows=new List<PlaylistSoundItem>(100000);
      for(int i=0;i<50000;i++)
      {
        rows.Add(new PlaylistSoundItem {Id=2*i+1,IdReferencedItem=2,OrderInPlaylist=2*i+1});
        rows.Add(new PlaylistSoundItem {IdReferencedItem=1,OrderInPlaylist=2*i+2});
      }
      var addedActual=rows[99999];
      var input=new SoundItemFilePlaylist {Id=1,Name="Mixed",HashCode=999,PlaylistItems=rows,
        ActualItem=addedActual,LastItemIndex=99999};
      var updated=f.Save(input);
      Assert.True(addedActual.Id>100000);
      Assert.Equal(addedActual.Id,updated.ActualItemId);
      using var context=f.ReadContext();
      var stored=context.PlaylistSongs.AsNoTracking().OrderBy(x=>x.OrderInPlaylist).ToArray();
      Assert.Equal(100000,stored.Length);
      Assert.Equal(Enumerable.Range(1,100000),stored.Select(x=>x.OrderInPlaylist));
      Assert.Equal(Enumerable.Range(0,50000).Select(x=>2*x+1),stored.Where(x=>x.Id<=100000).Select(x=>x.Id));
      Assert.Equal(50000,stored.Count(x=>x.Id>100000));
      Assert.Equal(100000,stored.Select(x=>x.Id).Distinct().Count());
      Assert.Equal(rows.Select(x=>x.Id),stored.Select(x=>x.Id));
      Assert.Equal(rows.Select(x=>x.IdReferencedItem),stored.Select(x=>x.IdReferencedItem));
      var root=context.Set<SoundItemFilePlaylist>().Single(x=>x.Id==1);
      Assert.Equal(addedActual.Id,root.ActualItemId);
      Assert.Equal(100000,root.ItemCount);
      Assert.Equal(2,context.SoundItems.Count());
      Assert.Equal(1,f.Disposals);
      Assert.True(f.DetectChangesRestored);
    }

    [Fact]
    public void FailureAfterCommittedRowBatchesRollsBackHugeQueueAndPlaybackMetadata()
    {
      using var f=new Fixture();
      // A parent write and two real row batches succeed before the injected failure.
      f.FailWriteNumber=4;
      var rows=Enumerable.Range(1,100000).Select(id=>new PlaylistSoundItem
        {Id=id,IdReferencedItem=1+id%2,OrderInPlaylist=100001-id}).ToList();
      var input=new SoundItemFilePlaylist {Id=1,Name="Must roll back",HashCode=888,PlaylistItems=rows,
        ActualItem=rows[43210],ActualItemId=rows[43210].Id,LastItemIndex=123};
      Assert.Throws<InvalidOperationException>(()=>f.Storage.UpdatePlaylist<SoundItemFilePlaylist,PlaylistSoundItem,SoundItem>(input,out _));
      using var context=f.ReadContext();
      var root=context.Set<SoundItemFilePlaylist>().Single(x=>x.Id==1);
      Assert.Equal("Original",root.Name);
      Assert.Equal(777,root.HashCode);
      Assert.Equal(100000,root.ItemCount);
      Assert.Equal(100000,root.ActualItemId);
      Assert.Equal(100000,context.PlaylistSongs.Count());
      Assert.False(context.PlaylistSongs.Any(x=>x.OrderInPlaylist!=x.Id || x.IdReferencedItem!=1+x.Id%2));
      Assert.Equal(4,f.Writes);
      Assert.Equal(1,f.Disposals);
      Assert.True(f.DetectChangesRestored);
    }

    [Fact]
    public void UnchangedHugeQueueReturnsStoredMetadataAndRepairsBarePlaybackReference()
    {
      using var f=new Fixture();
      var rows=Enumerable.Range(1,100000).Select(id=>new PlaylistSoundItem
        {Id=id,IdReferencedItem=1+id%2,OrderInPlaylist=id}).ToList();
      var input=new SoundItemFilePlaylist {Id=1,Name="Original",HashCode=777,PlaylistItems=rows,
        ActualItem=rows[54321],ActualItemId=rows[54321].Id};
      var updated=f.Save(input);
      Assert.True(updated.PlaylistItems.All(row=>row.ReferencedItem?.FileInfoEntity!=null));
      Assert.NotNull(updated.ActualItem.ReferencedItem?.FileInfoEntity);
      Assert.NotNull(input.ActualItem.ReferencedItem?.FileInfoEntity);
      Assert.Same(updated.ActualItem,input.ActualItem);
      using var context=f.ReadContext();
      Assert.Equal(100000,context.PlaylistSongs.Count());
      Assert.False(context.PlaylistSongs.Any(x=>x.OrderInPlaylist!=x.Id || x.IdReferencedItem!=1+x.Id%2));
      Assert.Equal(2,context.SoundItems.Count());
      Assert.Equal(1,f.Disposals);
      Assert.True(f.DetectChangesRestored);
    }

    [Fact]
    public void ReversingHugeQueuePersistsEveryRowIdentityPositionAndActualOccurrence()
    {
      using var f=new Fixture();
      var input=f.Read();
      var rows=input.PlaylistItems.OrderByDescending(x=>x.OrderInPlaylist).ToList();
      for(int i=0;i<rows.Count;i++) rows[i].OrderInPlaylist=i+1;
      input.PlaylistItems=rows;
      input.HashCode=888;
      input.ActualItem=rows[54321];
      input.ActualItemId=input.ActualItem.Id;
      input.LastItemIndex=54321;
      var expectedIds=rows.Select(x=>x.Id).ToArray();
      var expectedTracks=rows.Select(x=>x.IdReferencedItem).ToArray();
      f.Save(input);
      var stored=f.Read();
      var actual=stored.PlaylistItems.OrderBy(x=>x.OrderInPlaylist).ThenBy(x=>x.Id).ToArray();
      Assert.Equal(expectedIds,actual.Select(x=>x.Id));
      Assert.Equal(expectedTracks,actual.Select(x=>x.IdReferencedItem));
      Assert.Equal(Enumerable.Range(1,100000),actual.Select(x=>x.OrderInPlaylist));
      Assert.Equal(input.ActualItemId,stored.ActualItemId);
      Assert.Equal(54321,stored.LastItemIndex);
      Assert.Equal(100000,stored.ItemCount);
      Assert.NotNull(stored.ActualItem.ReferencedItem);
      Assert.All(actual,x=>Assert.NotNull(x.ReferencedItem.FileInfoEntity));
      Assert.Equal(1,f.Disposals);
      Assert.True(f.DetectChangesRestored);
    }
  }
}