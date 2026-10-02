using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Logger;
using Moq;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels;
using VPlayer.Home.ViewModels.LibraryViewModels;
using VPlayer.WindowsPlayer.ViewModels;
using VPLayer.Domain.Text;
using Xunit;
using Model=VPlayer.Tests.LibraryFilterTests.Model;

namespace VPlayer.Tests
{
  [Collection("UI synchronization")]
  public class FuzzySearchTests
  {
    private sealed class NameView : NamedEntityViewModel<Model>
    {
      public NameView(Model model):base(model) {}
      public override void Update(Model model)=>RefreshModel(model);
    }
    private static bool Legacy(string left,string right)=>left.Similarity(right)>0.8;

    [Fact]
    public void NullEmptyAndFloatThresholdBehaviorIsRetained()
    {
      Assert.False(FuzzySearch.IsSimilar(null,null));
      Assert.False(FuzzySearch.IsSimilar("",null));
      Assert.True(FuzzySearch.IsSimilar("",""));
      Assert.False(FuzzySearch.IsSimilar("","a"));
      // The legacy float for 4/5 is slightly greater than the double literal 0.8.
      Assert.True(Legacy("abcde","xbcde"));
      Assert.True(FuzzySearch.IsSimilar("abcde","xbcde"));
      Assert.False(FuzzySearch.IsSimilar("abcde","xycde"));
      Assert.False(FuzzySearch.IsSimilar("abcd","xbcd"));
      for(int length=1;length<=128;length++)
        for(int edits=Math.Max(0,length/5-1);edits<=Math.Min(length,length/5+1);edits++)
        {
          string left=new string('a',length),right=new string('b',edits)+new string('a',length-edits);
          Assert.Equal(Legacy(left,right),FuzzySearch.IsSimilar(left,right));
          Assert.Equal(Legacy(left,right+"xyz"),FuzzySearch.IsSimilar(left,right+"xyz"));
        }
    }

    [Fact]
    public void ExhaustiveShortInputsAndRandomUnicodeMatchLegacyDistance()
    {
      var words=new[] {""};
      var all=words.ToList();
      for(int length=1;length<=4;length++)
      {
        words=words.SelectMany(word=>new[] {word+"a",word+"b",word+"é"}).ToArray();
        all.AddRange(words);
      }
      foreach(var left in all)
        foreach(var right in all)
          Assert.Equal(Legacy(left,right),FuzzySearch.IsSimilar(left,right));
      var random=new Random(823741);
      const string alphabet="abcABCéÉİıΣσς \u0301\ud83d\ude80";
      string Word(int length)=>new string(Enumerable.Range(0,length).Select(_=>alphabet[random.Next(alphabet.Length)]).ToArray());
      for(int i=0;i<2000;i++)
      {
        var left=Word(random.Next(129));
        var right=i%2==0?Word(random.Next(129)):left;
        if(i%2!=0 && right.Length>0)
        {
          var chars=right.ToCharArray();
          for(int j=0;j<Math.Max(1,chars.Length/5);j++) chars[random.Next(chars.Length)]=alphabet[random.Next(alphabet.Length)];
          right=new string(chars);
        }
        Assert.Equal(Legacy(left,right),FuzzySearch.IsSimilar(left,right));
      }
    }

    [Fact]
    public void LongPrefixAndSuffixKeepTheOriginalSimilarityDenominator()
    {
      var left=new string('a',512)+"middle"+new string('z',512);
      foreach(var right in new[] {new string('a',512)+"different"+new string('z',512),left.Substring(0,800),new string('b',left.Length)})
        Assert.Equal(Legacy(left,right),FuzzySearch.IsSimilar(left,right));
    }

    [Fact]
    public void RepeatedLongNonmatchesDoNotAllocateDistanceMatrices()
    {
      var left=new string('a',1024);var right=new string('b',1024);
      Assert.False(FuzzySearch.IsSimilar(left,right));
      long before=GC.GetAllocatedBytesForCurrentThread();
      for(int i=0;i<16;i++) Assert.False(FuzzySearch.IsSimilar(left,right));
      Assert.InRange(GC.GetAllocatedBytesForCurrentThread()-before,0,256*1024);
    }

    [Fact]
    public void TenThousandLibraryTitlesRetainOrderAndObserveNameChanges()
    {
      LibraryLoadingTests.WithDispatcher(() =>
      {
        var views=Enumerable.Range(1,10000).Select(i=>new NameView(new Model {Id=i,Name="Q"+new string('a',90)+" "+i.ToString("D5")})).ToArray();
        var storage=new Mock<IStorageManager>();
        storage.Setup(x=>x.GetTempRepository<Model>()).Returns(Array.Empty<Model>().AsQueryable());
        var library=new LibraryCollection<NameView,Model>(new Mock<IViewModelsFactory>().Object,storage.Object,new Mock<ILogger>().Object)
          {Items=new RxObservableCollection<NameView>(views)};
        string query="Q"+new string('a',88)+"b 01234";
        var expected=views.Where(x=>x.Name.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0 || Legacy(x.Name.ToLowerInvariant(),query.ToLowerInvariant())).Select(x=>x.ModelId).ToArray();
        library.Filter(query);
        Assert.Equal(expected,library.FilteredItems.Select(x=>x.ModelId));
        Assert.Equal(expected,library.FilteredItemsCollection.Select(x=>x.ModelId));
        views[0].Update(new Model {Id=1,Name="Completely unrelated"});
        library.Filter(query);
        Assert.Equal(expected.Where(id=>id!=1),library.FilteredItemsCollection.Select(x=>x.ModelId));
        foreach(var view in views) view.Dispose();
        return System.Threading.Tasks.Task.CompletedTask;
      });
    }

    [Fact]
    public void PlayerPredicatePreservesCultureContainsAndFuzzyOnlyModes()
    {
      // This predicate reads no instance state; bypass media/device construction.
      var player=FormatterServices.GetUninitializedObject(typeof(MusicPlayerViewModel));
      var method=typeof(MusicPlayerViewModel).GetMethod("IsInFind",BindingFlags.Instance|BindingFlags.NonPublic);
      Assert.NotNull(method);
      var prior=CultureInfo.CurrentCulture;
      try
      {
        foreach(var culture in new[] {"en-US","tr-TR","el-GR"})
        {
          CultureInfo.CurrentCulture=new CultureInfo(culture);
          foreach(var left in new[] {null,"","I","İ","ı","ALPHA Song","abcde","Σσς"})
            foreach(var right in new[] {null,"","I","alpha","xbcde","Σσς"})
              foreach(var contains in new[] {true,false})
              {
                string phrase=right?.ToLower();
                bool expected=left!=null && phrase!=null && ((contains && left.ToLower().Contains(phrase)) || Legacy(left,phrase));
                Assert.Equal(expected,(bool)method.Invoke(player,new object[] {left,right,contains}));
              }
        }
      }
      finally {CultureInfo.CurrentCulture=prior;}
    }
  }
}
