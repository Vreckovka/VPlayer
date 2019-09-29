using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VCore;
using VCore.Modularity.Navigation;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VPlayer.AudioStorage.AudioInfoDownloader.Models;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
  public class AlbumCoversViewModel : RegionViewModel<AlbumCoversView>
  {
    #region Fields

    private readonly AlbumViewModel albumViewModel;
    private readonly INavigationProvider navigationProvider;
    private readonly IStorageManager storage;
    private object baton = new object();
    private CancellationTokenSource cancellationTokenSource;

    #endregion Fields

    #region Constructors

    public AlbumCoversViewModel(
      IRegionProvider regionProvider,
      AudioInfoDownloader.AudioInfoDownloader audioInfoDownloader,
      AlbumViewModel albumViewModel,
      IStorageManager storage,
      INavigationProvider navigationProvider) : base(regionProvider)
    {
      this.albumViewModel = albumViewModel ?? throw new ArgumentNullException(nameof(albumViewModel));
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
      this.navigationProvider = navigationProvider ?? throw new ArgumentNullException(nameof(navigationProvider));

      cancellationTokenSource = new CancellationTokenSource();
      var cancellationToken = cancellationTokenSource.Token;

      audioInfoDownloader.GetAlbumFrontCoversUrls(albumViewModel.Model, cancellationToken);

      audioInfoDownloader.CoversDownloaded += Instance_CoversDownloaded;
    }

    #endregion Constructors

    #region Properties

    public ObservableCollection<AlbumCover> AlbumCovers { get; set; } = new ObservableCollection<AlbumCover>();
    public override bool ContainsNestedRegions => false;
    public double DownloadedProcessValue { get; set; }
    public int FoundConvers { get; set; }
    public override string RegionName => RegionNames.LibraryContentRegion;

    #endregion Properties

    #region SelectCover

    private ActionCommand<AlbumCover> selectCover;

    public ICommand SelectCover
    {
      get
      {
        if (selectCover == null)
        {
          selectCover = new ActionCommand<AlbumCover>(OnSelectCover);
        }

        return selectCover;
      }
    }

    private void OnSelectCover(AlbumCover albumCover)
    {
      albumViewModel.Model.AlbumFrontCoverBLOB = albumCover.DownloadedCover;
      albumViewModel.Model.AlbumFrontCoverURI = albumCover.Url;

      storage.UpdateEntity(albumViewModel.Model);
    }

    #endregion SelectCover

    #region Methods

    #region GetFileSize

    public async Task<long> GetFileSize(string url)
    {
      long result = 0;

      WebRequest req = WebRequest.Create(url);
      req.Method = "HEAD";
      using (WebResponse resp = req.GetResponse())
      {
        if (long.TryParse(resp.Headers.Get("Content-Length"), out long contentLength))
        {
          result = contentLength;
        }
      }

      return result;
    }

    #endregion GetFileSize

    #region Instance_CoversDownloaded

    private async void Instance_CoversDownloaded(object sender, List<AlbumCover> e)
    {
      FoundConvers += e.Count;

      foreach (var cover in e)
      {
        var downloadedCover = await Task.Run(() =>
        {
          using (var client = new WebClient())
          {
            try
            {
              return client.DownloadData(cover.Url);
            }
            catch (Exception ex)
            {
              Logger.Logger.Instance.LogException(ex);
              return null;
            }
          }
        });

        if (downloadedCover != null)
          Application.Current.Dispatcher.Invoke(() =>
          {
            cover.DownloadedCover = downloadedCover;
            cover.Size = downloadedCover.Length;
            AlbumCovers.Add(cover);
            DownloadedProcessValue = (double)(AlbumCovers.Count * 100) / FoundConvers;
          });
      }
    }

    #endregion Instance_CoversDownloaded

    protected override void OnBackCommand()
    {
      base.OnBackCommand();

      cancellationTokenSource.Cancel();
    }

    #endregion Methods
  }
}