using System;
using System.Threading.Tasks;
using System.Windows.Input;
using IPTVStalker;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPLayer.Domain.Contracts.IPTV;
using VPlayer.IPTV.ViewModels;

namespace VPlayer.IPTV
{
  public class TvChannelItemGroupViewModel : TvChannelViewModel<TvChannelGroupItem>, ITvChannel
  {
    private readonly IStorageManager storageManager;
    private readonly IWindowManager windowManager;
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IIptvStalkerServiceProvider iptvStalkerServiceProvider;

    public TvChannelItemGroupViewModel(
      TvChannelGroupItem model,
      IStorageManager storageManager,
      IWindowManager windowManager,
      IViewModelsFactory viewModelsFactory,
      IIptvStalkerServiceProvider iptvStalkerServiceProvider) : base(model)
    {

      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.iptvStalkerServiceProvider = iptvStalkerServiceProvider ?? throw new ArgumentNullException(nameof(iptvStalkerServiceProvider));

      Name = model.TvChannel?.Name;

    }

    #region Commands

    #region Delete

    private ActionCommand delete;

    public ICommand Delete
    {
      get
      {
        if (delete == null)
        {
          delete = new ActionCommand(OnDelete);
        }

        return delete;
      }
    }

    public void OnDelete()
    {
      var question = windowManager.ShowYesNoPrompt($"Do you really want to remove  {Name} from group?", "Remove from tv group");

      if (question == System.Windows.MessageBoxResult.Yes)
      {
        var result = storageManager.DeleteEntity(Model);
      }
    }

    #endregion

    #endregion

    #region TvChannel

    private TvChannelViewModel tvChannelViewModel;

    public TvChannelViewModel TvChannel
    {
      get { return tvChannelViewModel; }
      set
      {
        if (value != tvChannelViewModel)
        {
          tvChannelViewModel = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TvChannelName

    public string TvChannelName
    {
      get { return Model?.TvChannel?.TvSource?.Name; }

    }

    #endregion

    #region IsSelectedToPlay

    private bool isSelectedToPlay;

    public bool IsSelectedToPlay
    {
      get { return isSelectedToPlay; }
      set
      {
        if (value != isSelectedToPlay)
        {
          isSelectedToPlay = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      if (Model.TvChannel?.TvSource != null)
      {
        switch (Model.TvChannel.TvSource.TvSourceType)
        {
          case TVSourceType.IPTVStalker:
            var split = Model.TvChannel.TvSource.SourceConnection.Split("|");

            if (split.Length == 2)
            {
              var url = split[0];
              var macAddress = split[1];

              var stalkerService = iptvStalkerServiceProvider.GetStalkerService(url, macAddress);

              TvChannel = viewModelsFactory.Create<TvStalkerChannelViewModel>(Model.TvChannel, stalkerService);
            }

            break;
          case TVSourceType.Source:
          case TVSourceType.M3U:
            TvChannel = viewModelsFactory.Create<TvChannelViewModel>(Model.TvChannel);
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
      }
    }

    #endregion

    public async Task<string> InitilizeUrl()
    {
      if (Model.TvChannel?.TvSource != null)
      {
        Url = await TvChannel.InitilizeUrl();

        return Url;
      }

      return null;
    }

    public override void RefreshSource()
    {
      TvChannel.RefreshSource();
    }


  }
}