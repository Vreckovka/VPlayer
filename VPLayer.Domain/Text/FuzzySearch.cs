using System;
using System.Buffers;

namespace VPLayer.Domain.Text
{
  public static class FuzzySearch
  {
    // Preserve StringHelper.Similarity's invariant casing, float arithmetic and
    // strict > 0.8 comparison, including the float value at the 80% boundary.
    public static bool IsSimilar(string original,string phrase)
    {
      if(original==null || phrase==null) return false;
      int maximum=Math.Max(original.Length,phrase.Length);
      if(maximum==0) return true;
      int limit=(int)Math.Ceiling(maximum/5.0);
      while(limit>=0 && !Accepted(limit,maximum)) limit--;
      while(limit<maximum && Accepted(limit+1,maximum)) limit++;
      if(Math.Abs(original.Length-phrase.Length)>limit) return false;
      ReadOnlySpan<char> left=original.ToLowerInvariant().AsSpan();
      ReadOnlySpan<char> right=phrase.ToLowerInvariant().AsSpan();
      int prefix=0;
      while(prefix<left.Length && prefix<right.Length && left[prefix]==right[prefix]) prefix++;
      left=left.Slice(prefix);right=right.Slice(prefix);
      int suffix=0;
      while(suffix<left.Length && suffix<right.Length && left[left.Length-1-suffix]==right[right.Length-1-suffix]) suffix++;
      left=left.Slice(0,left.Length-suffix);right=right.Slice(0,right.Length-suffix);
      if(left.Length==0 || right.Length==0) return Math.Max(left.Length,right.Length)<=limit;
      if(left.Length>right.Length){var swap=left;left=right;right=swap;}
      int width=left.Length+1;
      var first=ArrayPool<int>.Shared.Rent(width);
      int[] second=null;
      try
      {
        second=ArrayPool<int>.Shared.Rent(width);
        var previous=first.AsSpan(0,width);
        var current=second.AsSpan(0,width);
        int infinity=limit+1;
        for(int j=0;j<width;j++) previous[j]=Math.Min(j,infinity);
        for(int i=1;i<=right.Length;i++)
        {
          int start=Math.Max(1,i-limit),end=Math.Min(left.Length,i+limit);
          current[0]=Math.Min(i,infinity);
          if(start>1) current[start-1]=infinity;
          if(end<left.Length) current[end+1]=infinity;
          int minimum=current[0];
          for(int j=start;j<=end;j++)
          {
            int cost=left[j-1]==right[i-1]?0:1;
            current[j]=Math.Min(Math.Min(previous[j]+1,current[j-1]+1),previous[j-1]+cost);
            minimum=Math.Min(minimum,current[j]);
          }
          if(minimum>limit) return false;
          var swap=previous;previous=current;current=swap;
        }
        return previous[left.Length]<=limit;
      }
      finally
      {
        ArrayPool<int>.Shared.Return(first);
        if(second!=null) ArrayPool<int>.Shared.Return(second);
      }
    }
    private static bool Accepted(int distance,int maximum)=>(1.0f-(float)distance/maximum)>0.8;
  }
}
