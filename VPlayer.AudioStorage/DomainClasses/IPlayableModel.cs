namespace VPlayer.Core.ViewModels
{
  public interface IPlayableModel
  {
    string DiskLocation { get; set; }
    int Duration { get; set; }
    int Id { get; set; }
    int Length { get; set; }
    string Name { get; set; }

    bool IsFavorite { get; set; }
  }
}