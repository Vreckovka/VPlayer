﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class DomainEntity : ITrackable
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