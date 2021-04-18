using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.TreeView;
using VPlayer.AudioStorage.DomainClasses.IPTV;

namespace VPlayer.IPTV.ViewModels
{
  public class TvChannelGroupPlaylistItemViewModel : TreeViewItemViewModel<TvPlaylistItem>
  {
    public TvChannelGroupPlaylistItemViewModel(TvPlaylistItem model, TvChannelGroup tvChannelGroup, IViewModelsFactory viewModelsFactory) : base(model)
    {
      Name = model.Name;

      TvChannelGroupViewModel = viewModelsFactory.Create<TvChannelGroupViewModel>(tvChannelGroup);

      foreach (var item in TvChannelGroupViewModel.SubItems.ViewModels)
      {
        SubItems.Add(item);
      }

      CanExpand = SubItems.View.Count > 0;
    }

    #region TvGroupViewModel

    private TvChannelGroupViewModel tvChannelGroupViewModel;

    public TvChannelGroupViewModel TvChannelGroupViewModel
    {
      get { return tvChannelGroupViewModel; }
      set
      {
        if (value != tvChannelGroupViewModel)
        {
          tvChannelGroupViewModel = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion
  }
}