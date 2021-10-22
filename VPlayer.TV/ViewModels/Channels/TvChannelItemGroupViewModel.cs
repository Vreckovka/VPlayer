using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using IPTVStalker;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels.Prompt;
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
      var question = windowManager.ShowDeletePrompt(Name, "Do you really want to remove ", " from group?", "Remove from tv group");

      if (question == PromptResult.Ok)
      {
        var result = storageManager.DeleteEntity(Model);
      }
    }

    #endregion

    #region TvChannelDropped

    private ActionCommand<object> tvChannelDropped;

    public ICommand TvChannelDropped
    {
      get
      {
        if (tvChannelDropped == null)
        {
          tvChannelDropped = new ActionCommand<object>(OnTvChannelDropped);
        }

        return tvChannelDropped;
      }
    }

    protected void OnTvChannelDropped(object dropData)
    {
      IDataObject data = dropData as IDataObject;

      if (null == data) return;

      var tvChannelViewModel = data.GetData(data.GetFormats()[0]) as TvChannelItemGroupViewModel;
    }

    #endregion

    #endregion

    #region Index

    public int Index
    {
      get { return Model.Index; }
      set
      {
        if (value != Model.Index)
        {
          Model.Index = value;
          RaisePropertyChanged();
        }
      }
    }

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

    public ITvItem TvItem
    {
      get
      {
        return Model?.TvChannel?.TvItem;
      }
    }

    #region Methods

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

    #region InitilizeUrl

    public CancellationTokenSource ActualCancellationTokenSource { get; private set; }

    public async Task<string> InitilizeUrl(CancellationTokenSource pCancellationToken = null)
    {
      if (Model.TvChannel?.TvSource != null)
      {
        if (pCancellationToken == null)
        {
          var src = new CancellationTokenSource();
          ActualCancellationTokenSource = src;
        }
        else
          ActualCancellationTokenSource = pCancellationToken;


        Url = await TvChannel.InitilizeUrl(ActualCancellationTokenSource);

        return Url;
      }

      return null;
    }

    #endregion

    public override void RefreshSource()
    {
      TvChannel.RefreshSource();
    }

    #endregion

    public ITvChannel SelectedTvChannel
    {
      get
      {
        return this;
      }
      set { }
    }

    public IEnumerable<ITvChannel> TvChannelsSources { get; set; }
  }
}