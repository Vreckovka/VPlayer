using VCore.Standard.ViewModels.TreeView;
using VPlayer.AudioStorage.DomainClasses.IPTV;

namespace VPlayer.IPTV
{
  public class TvChannelGroupViewModel : TreeViewItemViewModel<TvChannelGroup>
  {
    public TvChannelGroupViewModel(TvChannelGroup model) : base(model)
    {
    }

  }
}