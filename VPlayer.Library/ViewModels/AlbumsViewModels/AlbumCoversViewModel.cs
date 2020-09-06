using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VCore;
using VCore.Modularity.Navigation;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Models;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
  public class AlbumCoversViewModel : RegionViewModel<AlbumCoversView>
  {
    #region Fields

    private readonly AlbumViewModel albumViewModel;
    private readonly IStorageManager storage;
    private CancellationTokenSource cancellationTokenSource;

    #endregion Fields

    #region Constructors

    public AlbumCoversViewModel(
      IRegionProvider regionProvider,
      AudioInfoDownloader audioInfoDownloader,
      AlbumViewModel albumViewModel,
      IStorageManager storage,
      string regionName) : base(regionProvider)
    {
      this.albumViewModel = albumViewModel ?? throw new ArgumentNullException(nameof(albumViewModel));
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));

      cancellationTokenSource = new CancellationTokenSource();
      var cancellationToken = cancellationTokenSource.Token;

      audioInfoDownloader.GetAlbumFrontCoversUrls(albumViewModel.Model, cancellationToken);

      audioInfoDownloader.CoversDownloaded += Instance_CoversDownloaded;

      RegionName = regionName;
    }

    #endregion Constructors

    #region Properties

    public ObservableCollection<AlbumCover> AlbumCovers { get; set; } = new ObservableCollection<AlbumCover>();
    public override bool ContainsNestedRegions => false;
    public double DownloadedProcessValue { get; set; }
    public int FoundConvers { get; set; }
    public override string RegionName { get; protected set; } = RegionNames.LibraryContentRegion;

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
      throw new NotImplementedException();
      //albumViewModel.Model.AlbumFrontCoverBLOB = albumCover.DownloadedCover;
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

    private object batton = new object();
    private Semaphore semaphore = new Semaphore(5,5);
    private void Instance_CoversDownloaded(object sender, List<AlbumCover> e)
    {
      FoundConvers += e.Count;
      try
      {
        lock (batton)
        {
          Task.Run(async () =>
          {
            foreach (var cover in e)
            {
              try
              {
                using (var client = new WebClient())
                using (var registration = cancellationTokenSource.Token.Register(() => client.CancelAsync()))
                {
                  semaphore.WaitOne();

                  if (cancellationTokenSource.IsCancellationRequested)
                    return;

                  var downloadedCover = await client.DownloadDataTaskAsync(cover.Url);

                  if (downloadedCover != null)
                  {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                      cover.DownloadedCover = downloadedCover;
                      cover.Size = downloadedCover.Length;

                      AlbumCovers.Add(cover);

                      DownloadedProcessValue = (double) (AlbumCovers.Count * 100) / FoundConvers;
                    });
                  }

                  semaphore.Release();
                }
              }
              catch (WebException ex) when (ex.Status == WebExceptionStatus.RequestCanceled)
              {
                Logger.Logger.Instance.Log(Logger.MessageType.Inform, "Downloading was stopped");
              }
              catch (Exception ex)
              {
                Logger.Logger.Instance.Log(ex);
              }
            }
          }, cancellationTokenSource.Token);
        };
      }
      catch (WebException ex) when (ex.Status == WebExceptionStatus.RequestCanceled)
      {
        Logger.Logger.Instance.Log(Logger.MessageType.Inform, "Downloading was stopped");
      }
    }


    #endregion Instance_CoversDownloaded

    protected override void OnBackCommand()
    {
      base.OnBackCommand();

      cancellationTokenSource.Cancel();
    }

    public static string GetRegionName()
    {
      return RegionNames.LibraryContentRegion;
    }

    #endregion Methods
  }
}