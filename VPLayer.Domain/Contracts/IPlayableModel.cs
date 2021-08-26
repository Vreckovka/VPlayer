using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.Core.ViewModels
{
  public interface IPlayableModel<TModel> : IEntity
  where TModel : IPlayableModel
  {
    public TModel ItemModel { get; set; }
  }

  public interface IPlayableModel : IEntity
  {
    public string Source { get; set; }
    public string Name { get; set; }
    public bool IsFavorite { get; set; }
  }

  public interface IFilePlayableModel : IPlayableModel
  {
    int Duration { get; set; }
  }
}