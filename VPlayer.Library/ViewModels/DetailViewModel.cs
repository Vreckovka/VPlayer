using System;
using System.Windows.Input;
using VCore;
using VCore.Annotations;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Modularity.Interfaces;
using VCore.ViewModels;
using VPlayer.AudioStorage.InfoDownloader.Models;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;

namespace VPlayer.Library.ViewModels
{
  public abstract class DetailViewModel<TDetailView> : RegionViewModel<TDetailView> where TDetailView : class, IView
  {
    private readonly IStorageManager storageManager;

    protected DetailViewModel(
      IRegionProvider regionProvider,
      [NotNull] IStorageManager storageManager) : base(regionProvider)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
    }

    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;


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

    private async void OnDelete()
    {
     
    }


    #endregion SelectCover

    #endregion
  }
}