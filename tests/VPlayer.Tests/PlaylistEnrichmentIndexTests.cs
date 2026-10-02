using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VPlayer.WindowsPlayer.ViewModels;
using Xunit;

namespace VPlayer.Tests
{
  public class PlaylistEnrichmentIndexTests
  {
    private static Dictionary<int,int> Map(int[] rows,Func<int,int> id)
    {
      var method=typeof(MusicPlayerViewModel).GetMethod("FirstOccurrenceIndices",BindingFlags.Static|BindingFlags.NonPublic)
        .MakeGenericMethod(typeof(int));
      return (Dictionary<int,int>)method.Invoke(null,new object[] {rows,id});
    }
    [Fact]
    public void MetadataLookupKeepsTheLegacyFirstOccurrenceForEveryDuplicateId()
    {
      var rows=new[] {9,3,9,0,3,-1,-1};
      var original=rows.ToArray();
      var indices=Map(rows,id=>id);
      Assert.Equal(rows.Distinct().Count(),indices.Count);
      foreach(int id in rows.Distinct()) Assert.Equal(Array.IndexOf(rows,id),indices[id]);
      Assert.Equal(original,rows);
      Assert.Empty(Map(Array.Empty<int>(),id=>id));
    }
    [Fact]
    public void HundredThousandRowsRequireOneIdReadEachAndKeepFiftyThousandFirstOccurrences()
    {
      var rows=Enumerable.Range(0,100000).Select(i=>i%50000).ToArray();
      int reads=0;
      var indices=Map(rows,id=>{reads++;return id;});
      Assert.Equal(rows.Length,reads);
      Assert.Equal(50000,indices.Count);
      for(int id=0;id<50000;id++) Assert.Equal(id,indices[id]);
    }
  }
}
