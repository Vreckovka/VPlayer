using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using VCore;
using VCore.Modularity.Events;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.ViewModels;
using VCore.WPF.ItemsCollections;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.IPTV.ViewModels;
using VPlayer.IPTV.ViewModels.Prompts;
using VPlayer.IPTV.Views;
using VPlayer.IPTV.Views.Prompts;

namespace VPlayer.IPTV
{
  public class IPTVManagerViewModel : RegionViewModel<IPTVManagerView>
  {
    private readonly IWindowManager windowManager;
    private readonly IStorageManager storageManager;
    private readonly IViewModelsFactory viewModelsFactory;

    #region Constructors

    public IPTVManagerViewModel(
      IRegionProvider regionProvider,
      IWindowManager windowManager,
      IStorageManager storageManager,
      IViewModelsFactory viewModelsFactory) : base(regionProvider)
    {
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    #endregion

    #region Properties

    public override string Header => "IPTV Management";
    public override string RegionName { get; protected set; } = RegionNames.LibraryContentRegion;


    #region TVSources

    private ItemsViewModel<TVSourceViewModel> tVSources = new ItemsViewModel<TVSourceViewModel>();

    public ItemsViewModel<TVSourceViewModel> TVSources
    {
      get { return tVSources; }
      set
      {
        if (value != tVSources)
        {
          tVSources = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TVGroups

    private ItemsViewModel<TvChannelGroupViewModel> tVGroups = new ItemsViewModel<TvChannelGroupViewModel>();

    public ItemsViewModel<TvChannelGroupViewModel> TVGroups
    {
      get { return tVGroups; }
      set
      {
        if (value != tVGroups)
        {
          tVGroups = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region NewTvGroupName

    private string newTvGroupName;

    public string NewTvGroupName
    {
      get { return newTvGroupName; }
      set
      {
        if (value != newTvGroupName)
        {
          newTvGroupName = value;
          addNewTvGroup.RaiseCanExecuteChanged();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Commands

    #region AddNewSource

    private ActionCommand addNewSource;

    public ICommand AddNewSource
    {
      get
      {
        if (addNewSource == null)
        {
          addNewSource = new ActionCommand(OnAddNewSource);
        }

        return addNewSource;
      }
    }

    public async void OnAddNewSource()
    {
      var vm = viewModelsFactory.Create<AddNewSourcePromptViewModel>();

      windowManager.ShowPrompt<AddNewSourcePrompt>(vm);

      if (vm.PromptResult == VCore.WPF.ViewModels.Prompt.PromptResult.Ok)
      {
        var select = vm.SelectedTvSourceViewModel;

        await select.PrepareEntityForDb();

        storageManager.StoreEntity(select.Model, out var stored);

        TVSources.Add(select);
      }
    }

    #endregion

    #region AddNewTvGroup

    private ActionCommand addNewTvGroup;

    public ICommand AddNewTvGroup
    {
      get
      {
        if (addNewTvGroup == null)
        {
          addNewTvGroup = new ActionCommand(OnAddNewTvGroup, () => { return !string.IsNullOrEmpty(NewTvGroupName); });
        }

        return addNewTvGroup;
      }
    }

    public void OnAddNewTvGroup()
    {
      var channelGroup = new TvChannelGroup()
      {
        Name = NewTvGroupName
      };

      var group = viewModelsFactory.Create<TvChannelGroupViewModel>(channelGroup);

      NewTvGroupName = null;

      storageManager.StoreEntity(channelGroup, out var stored);

      TVGroups.Add(group);
    }

    #endregion

    #endregion

    #region Methods

    public override void Initialize()
    {
      base.Initialize();

      this.storageManager.SubscribeToItemChange<TvSource>(OnTvSourceChanged).DisposeWith(this);
    }

    #region OnActivation

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {

        var sources = storageManager.GetRepository<TvSource>().OrderBy(x => x.Name).ToList().Select(CreateTvSourceViewModel);

        foreach (var source in sources)
        {
          TVSources.Add(source);
        }

        var groups = storageManager.GetRepository<TvChannelGroup>()
          .Include(x => x.TvChannels).ThenInclude(x => x.TvChannel).ThenInclude(x => x.TvSource).ToList()
          .Select(x => viewModelsFactory.Create<TvChannelGroupViewModel>(x));

        foreach (var group in groups)
        {
          TVGroups.Add(group);
        }
      }
    }

    #endregion

    #region CreateTvSourceViewModel

    private TVSourceViewModel CreateTvSourceViewModel(TvSource tVSource)
    {
      switch (tVSource.TvSourceType)
      {
        case TVSourceType.Source:
          return viewModelsFactory.Create<SourceTvSourceViewModel>(tVSource);
        case TVSourceType.M3U:
          return viewModelsFactory.Create<M3UTvSourceViewModel>(tVSource);
        case TVSourceType.IPTVStalker:
          return viewModelsFactory.Create<IptvStalkerTvSourceViewModel>(tVSource);
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    #endregion

    #region OnTvSourceChanged

    private void OnTvSourceChanged(ItemChanged<TvSource> tvSource)
    {
      var vm = TVSources.ViewModels.SingleOrDefault(x => x.Model.Id == tvSource.Item.Id);


      switch (tvSource.Changed)
      {
        case Changed.Added:
          break;
        case Changed.Removed:
          if (vm != null)
            TVSources.Remove(vm);
          break;
        case Changed.Updated:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    #endregion

    #endregion
  }
}
