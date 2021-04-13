using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCore;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.ViewModels.TreeView;
using VCore.WPF.ItemsCollections;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.IPTV.ViewModels
{
  public abstract class TVSourceViewModel : ViewModel<TvSource>, ISelectable
  {
    protected readonly IStorageManager storageManager;
    protected readonly IViewModelsFactory viewModelsFactory;

    public TVSourceViewModel(
      TvSource tVSource, 
      TVPlayerViewModel player,
      IStorageManager storageManager,
      IViewModelsFactory viewModelsFactory) : base(tVSource)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      Player = player ?? throw new ArgumentNullException(nameof(player));
    }

    #region Properties

    #region Name

    public string Name
    {
      get { return Model.Name; }
      set
      {
        if (value != Model.Name)
        {
          Model.Name = value;
          ValidateIsValid();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region SourceType

    public TVSourceType TvSourceType => Model.TvSourceType;

    #endregion

    #region IsValid

    private bool isValid;

    public bool IsValid
    {
      get { return isValid; }
      set
      {
        if (value != isValid)
        {
          isValid = value;

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TvChannels

    private ItemsViewModel<TvChannelViewModel> tvChannels = new ItemsViewModel<TvChannelViewModel>();

    public ItemsViewModel<TvChannelViewModel> TvChannels
    {
      get { return tvChannels; }
      set
      {
        if (value != tvChannels)
        {
          tvChannels = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsSelected

    private bool isSelected;

    public bool IsSelected
    {
      get { return isSelected; }
      set
      {
        if (value != isSelected)
        {
          isSelected = value;

          if(isSelected && TvChannels.ViewModels.Count == 0)
          {
            LoadChannels();
          }

          OnSelected();

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public TVPlayerViewModel Player { get;  }



    #region ActualFilter

    private string actualFilter;

    public string ActualFilter
    {
      get { return actualFilter; }
      set
      {
        if (value != actualFilter)
        {
          actualFilter = value;

          TvChannels.Filter(x => IsInFind(x.Model.Name, actualFilter));

          RaisePropertyChanged();
        }
      }
    }

    #endregion




    #endregion

    #region Methods

    public override void Initialize()
    {
      base.Initialize();

      TvChannels.OnActualItemChanged.Subscribe(x => Player.ActualChannel = x).DisposeWith(this);
    }

    #region LoadChannels

    public virtual void LoadChannels()
    {
      var dbEntity = storageManager.GetRepository<TvSource>().Include(x => x.TvChannels).SingleOrDefault(x => x.Id == Model.Id);

      if (dbEntity != null)
      {
        foreach (var channel in dbEntity.TvChannels)
        {
          TvChannels.Add(viewModelsFactory.Create<TvChannelViewModel>(channel));
        }
      }
    }

    #endregion

    #endregion

    #region IsInFind

    protected bool IsInFind(string original, string phrase, bool useContains = true)
    {
      bool result = false;

      if (original != null)
      {
        var lowerVariant = original.ToLower();

        if (useContains)
        {
          result = lowerVariant.Contains(phrase);
        }

        return original.Similarity(phrase) > 0.8 || result;
      }

      return result;
    }

    #endregion

    public abstract Task PrepareEntityForDb();

    public abstract void ValidateIsValid();

    protected virtual void OnSelected()
    { }


  }
}
