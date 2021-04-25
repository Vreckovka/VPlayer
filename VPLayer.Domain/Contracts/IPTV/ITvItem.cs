using System;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels;

namespace VPlayer.IPTV.ViewModels
{
  public interface ITvItem : IPlayableModel
  {
    DateTime? Created { get; set; }
    DateTime? Modified { get; set; }
  }
}