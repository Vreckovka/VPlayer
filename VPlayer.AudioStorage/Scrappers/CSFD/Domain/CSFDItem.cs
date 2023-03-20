using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.AudioStorage.Scrappers.CSFD.Domain
{
  public enum RatingColor
  {
    LightGray,
    Gray,
    Blue,
    Red
  }
  public class CSFDItem
  {
    public string OriginalName { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public string ImagePath { get; set; }
    public byte[] Image { get; set; }
    public int? Year { get; set; }
    public RatingColor? RatingColor { get; set; }
    public int? Rating { get; set; }

    public string[] Generes { get; set; }
    public string[] Actors { get; set; }
    public string[] Directors { get; set; }
    public string[] Parameters { get; set; }
    public string[] Origin { get; set; }

    public string Description { get; set; }
  }

  public class CSFDItemEntity : DomainEntity
  {
    public string OriginalName { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public string ImagePath { get; set; }
    public byte[] Image { get; set; }
    public int? Year { get; set; }
    public RatingColor? RatingColor { get; set; }
    public int? Rating { get; set; }

    public string[] Generes { get; set; }
    public string[] Actors { get; set; }
    public string[] Directors { get; set; }
    public string[] Parameters { get; set; }
  }
}