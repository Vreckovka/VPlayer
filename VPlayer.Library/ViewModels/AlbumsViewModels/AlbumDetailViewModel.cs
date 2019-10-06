using System;
using System.Collections.Generic;
using System.Windows.Input;
using VCore;
using VCore.Factories;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
  public class AlbumDetailViewModel : RegionViewModel<AlbumDetailView>
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;

    #endregion Fields

    #region Constructors

    public AlbumDetailViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      AlbumViewModel album) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      ActualAlbum = album;
    }

    #endregion Constructors

    #region Properties

    public AlbumViewModel ActualAlbum { get; set; }
    public IEnumerable<Song> AlbumSongs => ActualAlbum.Model?.Songs;

    public override bool ContainsNestedRegions => false;
    public override string RegionName => RegionNames.LibraryContentRegion;

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
      var covers = viewModelsFactory.Create<AlbumCoversViewModel>(ActualAlbum);
      covers.IsActive = true;
    }

    #endregion GetCovers
  }
}