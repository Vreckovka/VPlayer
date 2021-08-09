using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using VCore;
using VCore.Modularity.Events;
using VCore.Modularity.RegionProviders;
using VCore.Standard;
using VCore.Standard.Modularity.Interfaces;
using VCore.ViewModels;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;

namespace VPlayer.Home.ViewModels
{
  public abstract class DetailViewModel<TViewModel, TModel, TDetailView> : RegionViewModel<TDetailView>
    where TDetailView : class, IView
    where TViewModel : class, IViewModel<TModel>
    where TModel : class,INamedEntity
  {
    private readonly IStorageManager storageManager;
    private readonly IWindowManager windowManager;

    protected DetailViewModel(
      IRegionProvider regionProvider,
      IStorageManager storageManager,
      TViewModel model,
      IWindowManager windowManager) : base(regionProvider)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      ViewModel = model ?? throw new ArgumentNullException(nameof(model));

      storageManager.ItemChanged.Where(x =>
        x.Item is TModel &&
        x.Changed == VCore.Modularity.Events.Changed.Updated).Subscribe(OnDbUpdate);
    }

    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;

    #region ViewModel

    private TViewModel viewModel;

    public TViewModel ViewModel
    {
      get { return viewModel; }
      set
      {
        if (value != viewModel)
        {
          viewModel = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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

    protected virtual async void OnDelete()
    {
      var result = windowManager.ShowYesNoPrompt($"Do you want to delete {ViewModel.Model.Name}", "Delete");

      if (result == System.Windows.MessageBoxResult.Yes)
      {
        var resultDelete = await Task.Run(() =>
        {
          return storageManager.DeleteEntity(ViewModel.Model);
        });

        if (resultDelete)
        {
          windowManager.ShowPrompt($"Item {ViewModel.Model.Name} was deleted", "Sucess");
          OnBackCommand();
        }
      }
    }


    #endregion

    #region Update

    private ActionCommand update;

    public ICommand Update
    {
      get
      {
        if (update == null)
        {
          update = new ActionCommand(OnUpdate);
        }

        return update;
      }
    }

    protected abstract void OnUpdate();

    #endregion

    #endregion

    protected virtual void OnDbUpdate(ItemChanged itemChanged)
    {
      ViewModel.Model = (TModel)itemChanged.Item;
    }
  }
}