using System;
using System.ComponentModel.DataAnnotations.Schema;
using VCore.Standard;
using VCore.Standard.Modularity.Interfaces;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.DomainClasses.Songs;
using VPlayer.Core.ViewModels;
using VPLayer.Domain.Contracts.IPTV;

namespace VPlayer.IPTV.ViewModels
{
  [Serializable]
  public class TvItem : DomainEntity, ITvItem, IUpdateable<TvItem>
  {
    public string Source { get; set; }
    public string Name { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsPrivate { get; set; }
    public TimeSpan TimePlayed { get; set; }

    public void Update(TvItem other)
    {
      throw new System.NotImplementedException();
    }

  
  }


  public class TvPlaylistItem : ItemInPlaylist<TvItem>, IUpdateable<TvPlaylistItem>, INamedEntity
  {
    public string Name { get; set; }

    public override bool Compare(IItemInPlaylist<TvItem> other)
    {
      return base.Compare(other) || Name != ((TvPlaylistItem)other).Name;

      
    }

    public void Update(TvPlaylistItem other)
    {
      throw new System.NotImplementedException();
    }
  }
}