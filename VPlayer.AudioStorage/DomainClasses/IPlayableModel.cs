namespace VPlayer.Core.ViewModels
{
  public interface IPlayableModel
  {
    string Source { get; set; }
    int Id { get; set; }
    string Name { get; set; }
    bool IsFavorite { get; set; }
  }

  public interface IFilePlayableModel : IPlayableModel
  {
    int Duration { get; set; }
  }
}