using System.Collections.Generic;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels.SoundItems;

namespace VPlayer.Core.Factories
{
  public interface IIncomingPlaylistViewsFactory
  {
    IEnumerable<SoundItemInPlaylistViewModel> CreateIncomingPlaylistViews(IEnumerable<PlaylistSoundItem> rows);
  }
}