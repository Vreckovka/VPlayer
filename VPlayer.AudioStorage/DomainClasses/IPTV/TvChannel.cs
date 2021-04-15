using System.ComponentModel.DataAnnotations.Schema;

namespace VPlayer.AudioStorage.DomainClasses.IPTV
{
  public interface ICopyable<TEntity>
  {
    TEntity Copy();
  }

  public class TvChannel : DomainEntity, INamedEntity, ICopyable<TvChannel>
  {
    public string Name { get; set; }

    [ForeignKey(nameof(TvSource))]
    public int IdTvSource { get; set; }
    public TvSource TvSource { get; set; }

    public string Url { get; set; }
    public TvChannel Copy()
    {
      return new TvChannel()
      {
        Name = Name,
        Id = Id,
        IdTvSource = IdTvSource,
        Url = Url
      };
    }
  }
}