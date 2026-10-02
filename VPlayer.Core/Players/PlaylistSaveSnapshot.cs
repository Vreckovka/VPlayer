using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using VCore.Standard.Helpers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Songs;
using VPlayer.AudioStorage.DomainClasses.Video;

namespace VPlayer.Core.ViewModels
{
  public static class PlaylistSaveSnapshot
  {
    private static readonly Func<object,object> copyObject=BuildCopy();

    public static TPlaylist Create<TPlaylist>(TPlaylist playlist)
    {
      if(ReferenceEquals(playlist,null)) return default;
      // These stored graphs contain rows, track scalars and file-info scalars.
      // Unknown derived types retain the original complete-graph serialization.
      if(playlist is SoundItemFilePlaylist sound && sound.GetType()==typeof(SoundItemFilePlaylist) &&
        Supported<PlaylistSoundItem,SoundItem>(sound.PlaylistItems,sound.ActualItem))
        return (TPlaylist)(object)CopyFilePlaylist<SoundItemFilePlaylist,PlaylistSoundItem,SoundItem>(sound);
      if(playlist is VideoFilePlaylist video && video.GetType()==typeof(VideoFilePlaylist) &&
        Supported<PlaylistVideoItem,VideoItem>(video.PlaylistItems,video.ActualItem))
        return (TPlaylist)(object)CopyFilePlaylist<VideoFilePlaylist,PlaylistVideoItem,VideoItem>(video);
      return playlist.DeepClone();
    }

    private static bool Supported<TRow,TModel>(List<TRow> rows,TRow actual)
      where TRow : ItemInPlaylist<TModel> where TModel : PlayableItem
    {
      bool SupportedRow(TRow row) => row==null || (row.GetType()==typeof(TRow) &&
        (row.ReferencedItem==null || (row.ReferencedItem.GetType()==typeof(TModel) &&
          (row.ReferencedItem.FileInfoEntity==null || row.ReferencedItem.FileInfoEntity.GetType()==typeof(FileInfoEntity)))));
      if(!SupportedRow(actual)) return false;
      if(rows!=null) foreach(var row in rows) if(!SupportedRow(row)) return false;
      return true;
    }

    private static TPlaylist CopyFilePlaylist<TPlaylist,TRow,TModel>(TPlaylist source)
      where TPlaylist : FilePlaylist<TRow> where TRow : ItemInPlaylist<TModel> where TModel : PlayableItem
    {
      var copies=new Dictionary<object,object>(ReferenceComparer.Instance);
      var result=Copy(source,copies);
      if(source.PlaylistItems!=null)
      {
        result.PlaylistItems=new List<TRow>(source.PlaylistItems.Count);
        foreach(var row in source.PlaylistItems) result.PlaylistItems.Add(CopyRow<TRow,TModel>(row,copies));
      }
      result.ActualItem=CopyRow<TRow,TModel>(source.ActualItem,copies);
      return result;
    }

    private static TRow CopyRow<TRow,TModel>(TRow row,Dictionary<object,object> copies)
      where TRow : ItemInPlaylist<TModel> where TModel : PlayableItem
    {
      if(row==null) return null;
      if(copies.TryGetValue(row,out var existing)) return (TRow)existing;
      var result=Copy(row,copies);
      result.ReferencedItem=Copy(row.ReferencedItem,copies);
      if(result.ReferencedItem!=null)
        result.ReferencedItem.FileInfoEntity=Copy(row.ReferencedItem.FileInfoEntity,copies);
      return result;
    }

    private static T Copy<T>(T source,Dictionary<object,object> copies) where T : class
    {
      if(source==null) return null;
      if(copies.TryGetValue(source,out var existing)) return (T)existing;
      var result=(T)copyObject(source);
      copies.Add(source,result);
      return result;
    }

    private static Func<object,object> BuildCopy()
    {
      var source=Expression.Parameter(typeof(object),"source");
      var method=typeof(object).GetMethod("MemberwiseClone",BindingFlags.Instance|BindingFlags.NonPublic);
      return Expression.Lambda<Func<object,object>>(Expression.Call(source,method),source).Compile();
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
      internal static readonly ReferenceComparer Instance=new ReferenceComparer();
      public new bool Equals(object left,object right)=>ReferenceEquals(left,right);
      public int GetHashCode(object value)=>RuntimeHelpers.GetHashCode(value);
    }
  }
}
