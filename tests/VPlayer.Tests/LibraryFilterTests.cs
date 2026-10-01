using System;
using System.Collections.Generic;
using System.Linq;
using Logger;
using Moq;
using VCore.ItemsCollections;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Home.ViewModels.LibraryViewModels;
using Xunit;

namespace VPlayer.Tests
{
  public class LibraryFilterTests
  {
    public class Model : INamedEntity {public int Id {get;set;} public string Name {get;set;}}
    private LibraryCollection<INamedEntityViewModel<Model>,Model> Create(params string[] names)
    {
      var storage=new Mock<IStorageManager>();
      storage.Setup(x=>x.GetTempRepository<Model>()).Returns(Array.Empty<Model>().AsQueryable());
      var library=new LibraryCollection<INamedEntityViewModel<Model>,Model>(new Mock<IViewModelsFactory>().Object,storage.Object,new Mock<ILogger>().Object);
      library.Items=new RxObservableCollection<INamedEntityViewModel<Model>>(names.Select(name=>
      {
        var vm=new Mock<INamedEntityViewModel<Model>>();
        vm.SetupGet(x=>x.Name).Returns(name);
        return vm.Object;
      }));
      return library;
    }
    [Theory]
    [InlineData("ALPHA")]
    [InlineData("alpha")]
    [InlineData("AlPhA")]
    public void SearchMatchesRegardlessOfCase(string query)
    {
      var library=Create("Alpha Song","Completely different",null);
      library.Filter(query);
      Assert.Single(library.FilteredItems);
      Assert.Equal("Alpha Song",library.FilteredItems.First().Name);
    }
    [Fact]
    public void SearchBeforeLoadProducesEmptyResult()
    {
      var library=Create();
      library.Items=null;
      library.Filter("anything");
      Assert.Empty(library.FilteredItems);
      Assert.Empty(library.FilteredItemsCollection);
    }
    [Fact]
    public void FilterResultsAreMaterializedOnce()
    {
      var library=Create("Alpha Song","Different Song");
      library.Filter("alpha");
      var results=Assert.IsAssignableFrom<IList<INamedEntityViewModel<Model>>>(library.FilteredItems);
      Assert.Single(results);
      Assert.Single(library.FilteredItemsCollection);
      Assert.Same(results[0],library.FilteredItemsCollection[0]);
    }
    [Fact]
    public void RecreateSortsSnapshotOnceAndHonorsTake()
    {
      var library=Create("Zulu","Bravo","Alpha");
      library.MaxTake=2;
      library.Recreate();
      Assert.Equal(new[] {"Alpha","Bravo"},library.FilteredItems.Select(x=>x.Name));
      Assert.Equal(new[] {"Alpha","Bravo"},library.FilteredItems.Select(x=>x.Name));
    }
  }
}
