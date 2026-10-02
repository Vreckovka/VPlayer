using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Factories;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.TestSupport;
using Xunit;

namespace VPlayer.Tests
{
  public class IncomingPlaylistBatchTests
  {
    private static PlaylistSoundItem[] Rows()
    {
      var sounds=Enumerable.Range(1,97).Select(id=>new SoundItem {Id=id,Duration=id+20,
        FileInfoEntity=id==97?null:new FileInfoEntity {Title="Stored "+id,Name="Stored "+id}}).ToArray();
      return Enumerable.Range(0,100000).Select(i=>new PlaylistSoundItem {Id=i+1,OrderInPlaylist=i/2,ReferencedItem=sounds[i%97]}).ToArray();
    }
    [Fact]
    public void HugeDuplicateQueueKeepsMetadataIndependentStateAndEventOwnership()
    {
      using var fixture=new SavedSongViewFixture();
      var rows=Rows();
      var published=new List<SoundItemInPlaylistViewModel>();
      fixture.Events.GetEvent<PlaySongsFromPlayListEvent<SoundItemInPlaylistViewModel>>().Subscribe(published.Add);
      var views=fixture.Factory.CreateIncomingPlaylistViews(rows).ToArray();
      try
      {
        Assert.Equal(100000,views.Length);
        Assert.Equal(100000,new HashSet<object>(views).Count);
        for(int i=0;i<views.Length;i++)
        {
          Assert.Same(rows[i].ReferencedItem,views[i].Model);
          Assert.Equal(rows[i].ReferencedItem.Name,views[i].Name);
          Assert.Equal(rows[i].ReferencedItem.Duration,views[i].Duration);
          Assert.Same(rows[i].ReferencedItem.FileInfoEntity,views[i].Model.FileInfoEntity);
        }
        views[0].IsSelected=true;views[0].IsPlaying=true;
        Assert.False(views[97].IsSelected);Assert.False(views[97].IsPlaying);
        views[0].IsPlaying=false;
        views[0].Play.Execute(null);views[99999].Play.Execute(null);
        Assert.Equal(new[]{views[0],views[99999]},published);
      }
      finally {foreach(var view in views)view.Dispose();}
    }
    [Fact]
    public void ReenumeratingBuildsFreshOccurrenceState()
    {
      using var fixture=new SavedSongViewFixture();
      var sequence=fixture.Factory.CreateIncomingPlaylistViews(Rows());
      var first=sequence.ToArray();var second=sequence.ToArray();
      try
      {
        first[0].IsSelected=true;
        for(int i=0;i<first.Length;i++){Assert.NotSame(first[i],second[i]);Assert.Same(first[i].Model,second[i].Model);Assert.False(second[i].IsSelected);}
      }
      finally {foreach(var view in first.Concat(second))view.Dispose();}
    }
    [Fact]
    public void ExplicitViewActivationIsPreservedForEveryOccurrence()
    {
      using var fixture=new SavedSongViewFixture();
      int activations=0;
      fixture.Kernel.Bind<SoundItemInPlaylistViewModel>().ToSelf().OnActivation(view=>{activations++;view.IsSelected=true;});
      var views=fixture.Factory.CreateIncomingPlaylistViews(Rows()).ToArray();
      try {Assert.Equal(100000,activations);Assert.All(views,view=>Assert.True(view.IsSelected));}
      finally {foreach(var view in views)view.Dispose();}
    }
    [Fact]
    public void TransientEventServicesRetainPerOccurrencePublishers()
    {
      using var fixture=new SavedSongViewFixture();
      var publishers=new List<IEventAggregator>();
      var publications=new List<SoundItemInPlaylistViewModel>();
      fixture.Kernel.Rebind<IEventAggregator>().ToMethod(context=>{
        var events=new EventAggregator();publishers.Add(events);
        events.GetEvent<PlaySongsFromPlayListEvent<SoundItemInPlaylistViewModel>>().Subscribe(publications.Add);
        return events;
      });
      var views=fixture.Factory.CreateIncomingPlaylistViews(Rows()).ToArray();
      try
      {
        Assert.Equal(100000,publishers.Count);
        views[0].IsPlaying=false;
        views[0].Play.Execute(null);views[99999].Play.Execute(null);
        Assert.Equal(new[]{views[0],views[99999]},publications);
      }
      finally {foreach(var view in views)view.Dispose();}
    }
    [Fact]
    public void TransientStorageProviderIsInvokedForEveryOccurrence()
    {
      using var fixture=new SavedSongViewFixture();
      int resolutions=0;
      fixture.Kernel.Rebind<IStorageManager>().ToMethod(context=>{resolutions++;return fixture.Storage;});
      var views=fixture.Factory.CreateIncomingPlaylistViews(Rows()).ToArray();
      try {Assert.Equal(100000,resolutions);}
      finally {foreach(var view in views)view.Dispose();}
    }
    [Fact]
    public void ConditionalEventBindingReceivesTheOriginalViewInjectionContext()
    {
      using var fixture=new SavedSongViewFixture();
      var contextual=new EventAggregator();
      var publications=new List<SoundItemInPlaylistViewModel>();
      contextual.GetEvent<PlaySongsFromPlayListEvent<SoundItemInPlaylistViewModel>>().Subscribe(publications.Add);
      fixture.Kernel.Bind<IEventAggregator>().ToConstant(contextual).WhenInjectedInto<SoundItemInPlaylistViewModel>();
      var views=fixture.Factory.CreateIncomingPlaylistViews(Rows()).ToArray();
      try {views[0].Play.Execute(null);views[99999].Play.Execute(null);Assert.Equal(new[]{views[0],views[99999]},publications);}
      finally {foreach(var view in views)view.Dispose();}
    }
    private sealed class DerivedFactory : VPlayerViewModelsFactory, IViewModelsFactory
    {
      public DerivedFactory(IKernel kernel):base(kernel){}
      public int Creations {get;private set;}
      T IViewModelsFactory.Create<T>(params object[] parameters)
      {
        Creations++;
        var result=base.Create<T>(parameters);
        if(result is SoundItemInPlaylistViewModel view) view.IsSelected=true;
        return result;
      }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CustomFactoryReceivesEveryIncomingAndSavedCreation(bool saved)
    {
      using var fixture=new SavedSongViewFixture();
      var factory=new DerivedFactory(fixture.Kernel);
      var rows=Rows();
      var views=(saved?factory.CreateSavedSongViews(rows):factory.CreateIncomingPlaylistViews(rows)).ToArray();
      try {Assert.Equal(100000,factory.Creations);Assert.All(views,view=>Assert.True(view.IsSelected));}
      finally {foreach(var view in views)view.Dispose();}
    }
    [Fact]
    public void EmptySequenceNeedsNoContainerBindingsAndNullInputIsRejected()
    {
      using var kernel=new StandardKernel();
      var factory=new VPlayerViewModelsFactory(kernel);
      Assert.Empty(factory.CreateIncomingPlaylistViews(Array.Empty<PlaylistSoundItem>()));
      Assert.Throws<ArgumentNullException>(()=>factory.CreateIncomingPlaylistViews(null));
    }
  }
}