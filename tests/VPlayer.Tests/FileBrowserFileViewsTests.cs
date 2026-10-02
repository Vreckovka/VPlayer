using System;
using System.Linq;
using System.Reflection;
using Ninject;
using Moq;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Factories;
using VPlayer.Core.FileBrowser;
using VPlayer.TestSupport;
using Xunit;

namespace VPlayer.Tests
{
  public class FileBrowserFileViewsTests
  {
    private static FileInfo Info(int index)=>new FileInfo("copy-"+index+".mp3","copy-"+index+".mp3") {Name="Stored "+index,Length=index};
    private static object Service(PlayableFileViewModel view,string name)=>typeof(PlayableFileViewModel).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
    [Fact]
    public void HugeDirectoryKeepsMetadataAndIndependentFileViews()
    {
      using var fixture=new SavedSongViewFixture();
      var create=((IFileBrowserFileViewsFactory)fixture.Factory).CreateFileBrowserFileConstructor();
      var models=Enumerable.Range(0,100000).Select(Info).ToArray();
      var views=models.Select(create).ToArray();
      Assert.Equal(100000,views.Distinct().Count());
      Assert.Equal(100000,views.Select(x=>Service(x,"viewModelsFactory")).Distinct().Count());
      for(int i=0;i<views.Length;i++)
      {
        Assert.Same(models[i],views[i].Model);Assert.Equal(models[i].Name,views[i].Name);
        Assert.Same(fixture.Storage,Service(views[i],"storageManager"));
        Assert.Same(fixture.Events,Service(views[i],"eventAggregator"));
      }
      views[99999].IsSelected=true;Assert.False(views[0].IsSelected);
    }
    [Fact]
    public void ExplicitViewActivationStillRunsPerFile()
    {
      using var fixture=new SavedSongViewFixture();int activated=0;
      fixture.Kernel.Bind<PlayableFileViewModel>().ToSelf().OnActivation(view=>{activated++;view.Name="Custom "+view.Name;});
      var create=((IFileBrowserFileViewsFactory)fixture.Factory).CreateFileBrowserFileConstructor();
      var views=Enumerable.Range(0,1000).Select(i=>create(Info(i))).ToArray();
      Assert.Equal(1000,activated);Assert.All(views,x=>Assert.StartsWith("Custom ",x.Name));
    }
    [Fact]
    public void TransientEventServicesKeepTheirPerFileLifetime()
    {
      using var fixture=new SavedSongViewFixture();fixture.Kernel.Rebind<IEventAggregator>().To<EventAggregator>();
      var create=((IFileBrowserFileViewsFactory)fixture.Factory).CreateFileBrowserFileConstructor();
      var views=Enumerable.Range(0,1000).Select(i=>create(Info(i))).ToArray();
      Assert.Equal(1000,views.Select(x=>Service(x,"eventAggregator")).Distinct().Count());
    }
    [Fact]
    public void CustomFactoryInjectionKeepsItsRegisteredType()
    {
      using var fixture=new SavedSongViewFixture();fixture.Kernel.Rebind<IViewModelsFactory>().To<CustomFactory>();
      var create=((IFileBrowserFileViewsFactory)fixture.Factory).CreateFileBrowserFileConstructor();
      Assert.IsType<CustomFactory>(Service(create(Info(1)),"viewModelsFactory"));
    }
    public sealed class CustomFactory : VPlayerViewModelsFactory
    {
      public CustomFactory(IKernel kernel):base(kernel){}
    }
    [Fact]
    public void TransientWindowActivationIsNotBypassed()
    {
      using var fixture=new SavedSongViewFixture();int activated=0;
      fixture.Kernel.Rebind<IWindowManager>().ToMethod(context=>new Mock<IWindowManager>().Object).OnActivation(window=>activated++);
      var create=((IFileBrowserFileViewsFactory)fixture.Factory).CreateFileBrowserFileConstructor();
      for(int i=0;i<1000;i++)create(Info(i));Assert.Equal(1000,activated);
    }
  }
}
