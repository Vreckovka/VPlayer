using System;
using System.Collections.Generic;
using System.Windows.Input;
using VCore;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
  public class AlbumDetailViewModel : DetailViewModel<AlbumDetailView>
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IStorageManager storageManager;

    #endregion Fields

    #region Constructors

    public AlbumDetailViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      AlbumViewModel album,
      IStorageManager storageManager) : base(regionProvider, storageManager)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      ActualAlbum = album;
    }

    #endregion Constructors

    #region Properties

    public AlbumViewModel ActualAlbum { get; set; }
    public IEnumerable<Song> AlbumSongs => ActualAlbum.Model?.Songs;
    
    #endregion Properties

    #region GetCovers

    private ActionCommand getCovers;

    public ICommand GetCovers
    {
      get
      {
        if (getCovers == null)
        {
          getCovers = new ActionCommand(OnGetCovers);
        }

        return getCovers;
      }
    }

    protected void OnGetCovers()
    {
      var covers = viewModelsFactory.Create<AlbumCoversViewModel>(ActualAlbum,RegionName);

      covers.IsActive = true;
    }

    #endregion GetCovers
    
  }
}