using System.ComponentModel.DataAnnotations.Schema;
using VCore.Standard;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.Core.ViewModels;

namespace VPlayer.IPTV.ViewModels
{
  public class TvPlaylistItem : DomainEntity, IPlayableModel, IUpdateable<TvPlaylistItem>, IEntity
  {
    [ForeignKey(nameof(TvChannel))]
    public int IdTvChannel { get; set; }
    public TvChannel TvChannel { get; set; }
    public string Source { get; set; }
    public string Name { get; set; }
    public bool IsFavorite { get; set; }
   

    public void Update(TvPlaylistItem other)
    {
      throw new System.NotImplementedException();
    }
  }
}