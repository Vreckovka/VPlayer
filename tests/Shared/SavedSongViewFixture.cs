using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Logger;
using PCloudClient;
using Moq;
using Ninject;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Clients.MusixMatch;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Factories;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.TestSupport
{
  internal sealed class SavedSongViewFixture : IDisposable
  {
    internal readonly StandardKernel Kernel=new StandardKernel();
    internal readonly IEventAggregator Events=new EventAggregator();
    internal readonly IAlbumsViewModel Albums=new Mock<IAlbumsViewModel>().Object;
    internal readonly IArtistsViewModel Artists=new Mock<IArtistsViewModel>().Object;
    internal readonly IStorageManager Storage=new Mock<IStorageManager>().Object;
    internal readonly ILogger Logger=new Mock<ILogger>().Object;
    internal readonly IWindowManager Windows=new Mock<IWindowManager>().Object;
    internal readonly AudioInfoDownloader Downloader=Empty<AudioInfoDownloader>();
    internal readonly PCloudLyricsProvider Cloud;
    internal readonly MusixMatchLyricsProvider Lyrics=Empty<MusixMatchLyricsProvider>();
    internal readonly VPlayerViewModelsFactory Factory;
    private readonly MusicPlayerViewModel player=Empty<MusicPlayerViewModel>();
    private static readonly MethodInfo create=typeof(MusicPlayerViewModel).GetMethod("GetVmToPlayFromPlaylist",BindingFlags.Instance|BindingFlags.NonPublic);
    internal SavedSongViewFixture(bool transientWindows=false)
    {
      Cloud=new PCloudLyricsProvider(new Mock<IPCloudService>().Object,Windows,new Mock<IPCloudProvider>().Object);
      Kernel.Bind<IEventAggregator>().ToConstant(Events);
      Kernel.Bind<IAlbumsViewModel>().ToConstant(Albums);
      Kernel.Bind<IArtistsViewModel>().ToConstant(Artists);
      Kernel.Bind<IStorageManager>().ToConstant(Storage);
      Kernel.Bind<ILogger>().ToConstant(Logger);
      if(transientWindows)
        Kernel.Bind<IWindowManager>().To<WindowManager>()
          .WithMetadata("VPlayerSavedSongWindowConstructor",new Func<IWindowManager>(()=>new WindowManager()));
      else Kernel.Bind<IWindowManager>().ToConstant(Windows);
      Kernel.Bind<AudioInfoDownloader>().ToConstant(Downloader);
      Kernel.Bind<PCloudLyricsProvider>().ToConstant(Cloud);
      Kernel.Bind<MusixMatchLyricsProvider>().ToConstant(Lyrics);
      Kernel.Bind<IViewModelsFactory>().To<VPlayerViewModelsFactory>().WithMetadata("VPlayerDefaultViewFactory",true);
      Factory=new VPlayerViewModelsFactory(Kernel);
      var type=player.GetType();
      FieldInfo factory=null;
      while(type!=null && factory==null)
      {
        factory=type.GetField("viewModelsFactory",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
        type=type.BaseType;
      }
      if(factory==null || create==null) throw new InvalidOperationException("Saved playlist construction seam missing.");
      factory.SetValue(player,Factory);
    }
    internal IEnumerable<SoundItemInPlaylistViewModel> Create(IEnumerable<PlaylistSoundItem> rows) =>
      (IEnumerable<SoundItemInPlaylistViewModel>)create.Invoke(player,new object[]{rows});
    internal SongInPlayListViewModel CreateDirect(Song song) => new SongInPlayListViewModel(
      Events,Albums,Artists,Downloader,Cloud,song,Logger,Storage,Windows,Factory,Lyrics);
    private static T Empty<T>() where T:class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    public void Dispose()=>Kernel.Dispose();
  }
}
