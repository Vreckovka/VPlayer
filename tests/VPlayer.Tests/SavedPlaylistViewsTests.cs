using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.TestSupport;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using Prism.Events;
using Xunit;

namespace VPlayer.Tests
{
  public class SavedPlaylistViewsTests
  {
    [Fact]
    public void HugeDuplicateQueueKeepsStoredMetadataAndIndependentOccurrences()
    {
      using var fixture=new SavedSongViewFixture(true);
      var tracks=Enumerable.Range(1,97).Select(id=>new SoundItem {Id=id,Duration=id+20,
        FileInfoEntity=id==97?null:new FileInfoEntity {Name="Stored "+id,Source="file:///D:/stress/"+id+".mp3"}}).ToArray();
      var rows=Enumerable.Range(0,100000).Select(i=>new PlaylistSoundItem {Id=i+1,OrderInPlaylist=i+1,ReferencedItem=tracks[i%97]}).ToArray();
      var views=fixture.Create(rows).Cast<SongInPlayListViewModel>().ToArray();
      try
      {
        Assert.Equal(100000,views.Length);
        Assert.Equal(100000,new HashSet<SongInPlayListViewModel>(views).Count);
        Assert.Equal(100000,new HashSet<Song>(views.Select(x=>x.SongModel)).Count);
        var factories=typeof(SongInPlayListViewModel).GetField("viewModelsFactory",BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.Equal(100000,new HashSet<object>(views.Select(view=>factories.GetValue(view))).Count);
        var windows=typeof(SongInPlayListViewModel).GetField("windowManager",BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.Equal(100000,new HashSet<object>(views.Select(view=>windows.GetValue(view))).Count);
        for(int i=0;i<views.Length;i++)
        {
          Assert.Same(rows[i].ReferencedItem,views[i].Model);
          Assert.Same(rows[i].ReferencedItem,views[i].SongModel.ItemModel);
          Assert.Equal(rows[i].ReferencedItem.Name,views[i].Name);
          Assert.Equal(rows[i].ReferencedItem.Duration,views[i].Model.Duration);
        }
        views[0].IsPlaying=true;
        views[0].IsSelected=true;
        Assert.False(views[97].IsPlaying);
        Assert.False(views[97].IsSelected);
      }
      finally {foreach(var view in views)view.Dispose();}
    }
    [Fact]
    public void EachEnumerationBuildsFreshOccurrenceState()
    {
      using var fixture=new SavedSongViewFixture();
      var rows=new[]{new PlaylistSoundItem {ReferencedItem=new SoundItem {Id=1}}};
      var sequence=fixture.Create(rows);
      var first=Assert.Single(sequence);
      var second=Assert.Single(sequence);
      try {Assert.NotSame(first,second);Assert.Same(first.Model,second.Model);}
      finally {first.Dispose();second.Dispose();}
    }
    [Fact]
    public void TransientServicesAreResolvedForEachOccurrence()
    {
      using var fixture=new SavedSongViewFixture();
      int resolutions=0;
      fixture.Kernel.Rebind<IEventAggregator>().ToMethod(context=>{resolutions++;return new EventAggregator();});
      var rows=Enumerable.Range(1,97).Select(id=>new PlaylistSoundItem {ReferencedItem=new SoundItem {Id=id}}).ToArray();
      var views=fixture.Create(rows).ToArray();
      try {Assert.Equal(rows.Length,resolutions);}
      finally {foreach(var view in views)view.Dispose();}
    }
    [Fact]
    public void CustomWindowActivationRemainsPerOccurrence()
    {
      using var fixture=new SavedSongViewFixture(true);
      int activations=0;
      int wrongParents=0;
      fixture.Kernel.Rebind<IWindowManager>().To<WindowManager>()
        .WithMetadata("VPlayerSavedSongWindowConstructor",new Func<IWindowManager>(()=>new WindowManager()))
        .OnActivation((context,window)=>{activations++;if(context.Request.ParentContext?.Request.Service!=typeof(SongInPlayListViewModel))wrongParents++;});
      var rows=Enumerable.Range(1,97).Select(id=>new PlaylistSoundItem {ReferencedItem=new SoundItem {Id=id}}).ToArray();
      var views=fixture.Create(rows).ToArray();
      try {Assert.Equal(rows.Length,activations);Assert.Equal(0,wrongParents);}
      finally {foreach(var view in views)view.Dispose();}
    }
    [Fact]
    public void ExplicitViewBindingRetainsItsActivationBehavior()
    {
      using var fixture=new SavedSongViewFixture();
      int activations=0;
      fixture.Kernel.Bind<SongInPlayListViewModel>().ToSelf().OnActivation(view=>{activations++;view.IsSelected=true;});
      var rows=Enumerable.Range(1,97).Select(id=>new PlaylistSoundItem {ReferencedItem=new SoundItem {Id=id}}).ToArray();
      var views=fixture.Create(rows).ToArray();
      try {Assert.Equal(rows.Length,activations);Assert.All(views,view=>Assert.True(view.IsSelected));}
      finally {foreach(var view in views)view.Dispose();}
    }
  }
}
