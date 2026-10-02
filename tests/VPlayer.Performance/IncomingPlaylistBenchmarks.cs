using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Moq;
using VCore.Standard.Providers;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud;
using VPlayer.Home.ViewModels;
using VPlayer.Home.ViewModels.LibraryViewModels;
using VPlayer.TestSupport;
using VPLayer.Domain;

namespace VPlayer.Performance
{
  internal static class IncomingPlaylistBenchmarks
  {
    public static void Run(string directory,string output,string commit)
    {
      output=Path.GetFullPath(output);
      if(File.Exists(output)) throw new IOException("Use a new benchmark output.");
      if(commit.Length!=40 || commit.Any(c=>!Uri.IsHexDigit(c))) throw new ArgumentException("Exact source commit required.");
      var database=Path.Combine(Path.GetFullPath(directory),"VPlayerDatabase.db");
      var samples=new List<object>();
      string orderHash=null,metadataHash=null;
      int duplicates=0,missingInfo=0;
      for(int iteration=0;iteration<5;iteration++)
      {
        using var context=new FixtureContext(database);
        using var fixture=new SavedSongViewFixture();
        // Delay repository construction until production requests it, preserving first-use EF cost.
        Mock.Get(fixture.Storage).Setup(x=>x.GetTempRepository<SoundItemFilePlaylist>())
          .Returns(()=>context.SoundItemPlaylists.AsNoTracking());
        var collection=new LibraryCollection<SongsPlaylistViewModel,SoundItemFilePlaylist>(fixture.Factory,fixture.Storage,fixture.Logger);
        var owner=new SoundItemPlaylistsViewModel(new Mock<IRegionProvider>().Object,fixture.Factory,fixture.Storage,
          new Mock<ISettingsProvider>().Object,collection,fixture.Events);
        using var playlist=new SongsPlaylistViewModel(new SoundItemFilePlaylist {Id=658},fixture.Events,fixture.Factory,owner,
          new Mock<IVPlayerCloudService>().Object,fixture.Storage,fixture.Logger,fixture.Windows);
        var total=Stopwatch.StartNew();
        var incoming=playlist.GetItemsToPlay().GetAwaiter().GetResult();
        var read=total.Elapsed.TotalMilliseconds;
        if(incoming==null) throw new InvalidOperationException("Missing stress playlist.");
        var allocated=GC.GetAllocatedBytesForCurrentThread();
        var creation=Stopwatch.StartNew();
        var views=incoming.ToArray();
        creation.Stop();total.Stop();
        allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
        try
        {
          var rows=playlist.Model.PlaylistItems.OrderBy(x=>x.OrderInPlaylist).ThenBy(x=>x.Id).ToArray();
          if(rows.Length!=100000 || views.Length!=100000 || new HashSet<object>(views).Count!=100000)
            throw new InvalidOperationException("Incomplete or shared playlist occurrence state.");
          for(int i=0;i<views.Length;i++)
            if(!ReferenceEquals(rows[i].ReferencedItem,views[i].Model) || views[i].Name!=rows[i].ReferencedItem.Name || views[i].Duration!=rows[i].ReferencedItem.Duration)
              throw new InvalidOperationException("Incoming order, identity or stored metadata changed.");
          var order=HashText(string.Join("\n",rows.Select(x=>x.Id+"|"+x.OrderInPlaylist+"|"+x.IdReferencedItem)));
          var metadata=HashText(string.Join("\n",views.Select(x=>x.Model.Id+"|"+x.Name+"|"+x.Duration+"|"+x.Model.FileInfoEntity?.Title)));
          if(iteration>0 && (order!=orderHash || metadata!=metadataHash)) throw new InvalidOperationException("Stress results changed between iterations.");
          orderHash=order;metadataHash=metadata;
          duplicates=views.Length-views.Select(x=>x.Model.Id).Distinct().Count();
          missingInfo=views.Count(x=>x.Model.FileInfoEntity==null);
          samples.Add(new {Iteration=iteration,ReadMilliseconds=read,CreationMilliseconds=creation.Elapsed.TotalMilliseconds,
            TotalMilliseconds=total.Elapsed.TotalMilliseconds,CreationAllocatedBytes=allocated,OrderedIndependentOccurrences=true,StoredMetadataVerified=true});
        }
        finally {foreach(var view in views)view.Dispose();}
      }
      using var stream=File.OpenRead(database);
      using var hash=SHA256.Create();
      File.WriteAllText(output,JsonSerializer.Serialize(new {
        Schema="incoming-playlist-v1",Commit=commit,CreatedUtc=DateTime.UtcNow,Runtime=Environment.Version.ToString(),
        Configuration="Release",Architecture="x64",Environment.ProcessorCount,OS=Environment.OSVersion.VersionString,
        FixtureSha256=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-",""),PlaylistId=658,Entries=100000,DuplicateTracks=duplicates,MissingFileInfo=missingInfo,
        OrderedRowsSha256=orderHash,MetadataSha256=metadataHash,
        Boundary="Production SongsPlaylistViewModel.GetItemsToPlay plus enumeration; fresh read-only context per iteration, real default Ninject factory and shared stub services; verification/disposal/rendering excluded; creation allocations are current-thread only.",
        Samples=samples
      },new JsonSerializerOptions{WriteIndented=true}));
      Console.WriteLine("Verified 100k incoming rows, stored metadata and independent occurrences: "+output);
    }
    private static string HashText(string value)
    {
      using var hash=SHA256.Create();
      return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","");
    }
  }
}