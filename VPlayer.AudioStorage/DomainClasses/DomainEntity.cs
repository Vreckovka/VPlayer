using System;
using System.ComponentModel.DataAnnotations;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class DomainEntity : ITrackable, IEntity
  {
    [Key]
    public int Id { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Modified { get; set; }
  }

  public interface ITrackable
  {
    DateTime? Created { get; set; }
    DateTime? Modified { get; set; }
  }
}
