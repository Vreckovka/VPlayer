using System.Linq;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.AudioStorage.Scrappers.CSFD.Domain
{
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
    public string Country { get; set; }
    public string Length { get; set; }
    public string Premiere { get; set; }

    public string GeneresList
    {
      get
      {
        return Generes?.Aggregate((x, y) =>
        {
          if (!string.IsNullOrEmpty(y))
          {
            return $"{x}, {y}";
          }

          return x;
        });
      }
    }

    public string ActorsList
    {
      get
      {
        return Actors?.Aggregate((x, y) =>
        {
          if (!string.IsNullOrEmpty(y))
          {
            return $"{x}, {y}";
          }

          return x;
        });
      }
    }

    public string DirectorsList
    {
      get
      {
        return Directors?.Aggregate((x, y) =>
        {
          if (!string.IsNullOrEmpty(y))
          {
            return $"{x}, {y}";
          }

          return x;
        });
      }
    }

    public string ParametersList
    {
      get
      {
        return Parameters?.Aggregate((x, y) =>
        {
          if (!string.IsNullOrEmpty(y))
          {
            return $"{x}, {y}";
          }

          return x;
        });
      }
    }

    public string OriginList
    {
      get
      {
        return Origin?.Aggregate((x, y) =>
        {
          if (!string.IsNullOrEmpty(y))
          {
            return $"{x}, {y}";
          }

          return x;
        });
      }
    }
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