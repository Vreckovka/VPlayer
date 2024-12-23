﻿using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using VCore;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.Events;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.Modularity.Regions;

namespace VPlayer.Home.ViewModels
{
  public abstract class DetailViewModel<TViewModel, TModel, TDetailView> : RegionViewModel<TDetailView>
    where TDetailView : class, IView
    where TViewModel : class, IViewModel<TModel>
    where TModel : class,INamedEntity, IUpdateable<TModel>
  {
    private readonly IStorageManager storageManager;
    private readonly IStatusManager statusManager;
    private readonly IWindowManager windowManager;

    protected DetailViewModel(
      IRegionProvider regionProvider,
      IStorageManager storageManager,
      IStatusManager statusManager,
      TViewModel model,
      IWindowManager windowManager) : base(regionProvider)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
   
      ViewModel = model ?? throw new ArgumentNullException(nameof(model));
    }

    #region Properties

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
      var result = windowManager.ShowDeletePrompt(ViewModel.Model.Name);

      if (result == VCore.WPF.ViewModels.Prompt.PromptResult.Ok)
      {
        var resultDelete = await Task.Run(() =>
        {
          return storageManager.DeleteEntity(ViewModel.Model);
        });

        if (resultDelete)
        {
          windowManager.OkCancel($"Item {ViewModel.Model.Name} was deleted", "Sucess");

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

    protected virtual void OnUpdate()
    {

    }

    #endregion

    #region Save

    private ActionCommand save;

    public ICommand Save
    {
      get
      {
        if (save == null)
        {
          save = new ActionCommand(OnSave);
        }

        return save;
      }
    }

    protected  virtual async void OnSave()
    {
      if (await storageManager.UpdateEntityAsync(ViewModel.Model))
      {
        statusManager.ShowDoneMessage($"Album: {ViewModel.Model.Name} UPDATED");
      }
      else
      {
        statusManager.ShowFailedMessage($"Album: {ViewModel.Model.Name} FAILED TO UPDATED");
      }
     
    }

    #endregion

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      storageManager.OnItemChanged.OfType<IItemChanged<TModel>>().Where(x =>
        x.Changed == VCore.WPF.Modularity.Events.Changed.Updated).Subscribe(OnDbUpdate).DisposeWith(this);

      LoadEntity();
    }

    #endregion

    #region OnDbUpdate

    protected virtual void OnDbUpdate(IItemChanged<TModel> itemChanged)
    {
      if(itemChanged.Item.Id == ViewModel.Model.Id)
      {
        ViewModel.Model = itemChanged.Item;
      }
    }

    #endregion

    protected abstract Task LoadEntity();
    

    #endregion
  }
}