using System.Collections.Generic;
using System.ComponentModel;
using VCore.Standard.ViewModels.TreeView;
using VPlayer.Core.ViewModels;

namespace VPLayer.Domain.Contracts.IPTV
{
  public interface ITvPlayableItem : INotifyPropertyChanged, ISelectable
  {
    public ITvChannel SelectedTvChannel { get; set; }

    public IEnumerable<ITvChannel> TvChannelsSources { get; set; }
    
  }
}