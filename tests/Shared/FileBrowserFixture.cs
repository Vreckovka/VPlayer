using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Moq;
using Ninject;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Providers;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Factories;
using VPlayer.Home.ViewModels.FileBrowser;

namespace VPlayer.TestSupport
{
  // Real Windows browser/folder/file view models; only external services are mocked.
  internal sealed class FileBrowserFixture : IDisposable
  {
    private readonly StandardKernel kernel=new StandardKernel();
    private readonly SynchronizationContext previousUi=VSynchronizationContext.UISynchronizationContext;
    private readonly Dispatcher previousDispatcher=VSynchronizationContext.UIDispatcher;
    internal readonly Mock<IWindowManager> Windows=new Mock<IWindowManager>();
    internal WindowsFileBrowserViewModel Browser {get;}
    internal FileBrowserFixture(bool productionWindows=false)
    {
      if(Application.Current==null) new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
      VSynchronizationContext.UISynchronizationContext=new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher);
      VSynchronizationContext.UIDispatcher=Dispatcher.CurrentDispatcher;
      kernel.Bind<IEventAggregator>().ToConstant(new EventAggregator());
      kernel.Bind<IStorageManager>().ToConstant(new Mock<IStorageManager>().Object);
      if(productionWindows)
        kernel.Bind<IWindowManager>().To<WindowManager>().WithMetadata("VPlayerSavedSongWindowConstructor",new Func<IWindowManager>(()=>new WindowManager()));
      else kernel.Bind<IWindowManager>().ToConstant(Windows.Object);
      kernel.Bind<IViewModelsFactory>().To<VPlayerViewModelsFactory>().WithMetadata("VPlayerDefaultViewFactory",true);
      var factory=kernel.Get<IViewModelsFactory>();
      Browser=new WindowsFileBrowserViewModel(new Mock<IRegionProvider>().Object,factory,
        new Mock<ISettingsProvider>().Object,Windows.Object,new Mock<IStorageManager>().Object,
        Array.Empty<VPlayer.Core.ViewModels.IPlayableRegionViewModel>());
    }
    internal void Open(string directory)
    {
      Browser.OnBaseDirectoryPathChanged(directory);
      WaitUntil(()=>Browser.Items!=null,TimeSpan.FromMinutes(2));
    }
    internal static void WaitUntil(Func<bool> completed,TimeSpan timeout)
    {
      var watch=Stopwatch.StartNew();
      while(!completed())
      {
        if(watch.Elapsed>timeout)throw new TimeoutException("File browser operation did not complete.");
        LyricsAnimationFixture.Pump(TimeSpan.FromMilliseconds(5));
      }
    }
    internal static void Await(Task task,TimeSpan? timeout=null)
    {
      WaitUntil(()=>task.IsCompleted,timeout??TimeSpan.FromMinutes(5));
      task.GetAwaiter().GetResult();
    }
    public void Dispose()
    {
      Browser.Dispose();kernel.Dispose();
      VSynchronizationContext.UISynchronizationContext=previousUi;
      VSynchronizationContext.UIDispatcher=previousDispatcher;
    }
  }
}
