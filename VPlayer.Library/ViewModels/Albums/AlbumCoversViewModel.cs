using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Logger;
using VCore;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Models;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Home.Views.Music.Albums;
using VPlayer.Library;

namespace VPlayer.Home.ViewModels.Albums
{
  public class AlbumCoversViewModel : RegionViewModel<AlbumCoversView>
  {
    #region Fields

    private readonly AudioInfoDownloader audioInfoDownloader;
    private readonly AlbumViewModel albumViewModel;
    private readonly IStorageManager storage;
    private readonly ILogger logger;
    private CancellationTokenSource cancellationTokenSource;
    private string path;

    #endregion Fields

    #region Constructors

    public AlbumCoversViewModel(
      IRegionProvider regionProvider,
      AudioInfoDownloader audioInfoDownloader,
      AlbumViewModel albumViewModel,
      IStorageManager storage,
      string regionName,
      ILogger logger) : base(regionProvider)
    {
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
      this.albumViewModel = albumViewModel ?? throw new ArgumentNullException(nameof(albumViewModel));
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

      RegionName = regionName;
    }

    #endregion Constructors

    #region Properties

    public ObservableCollection<AlbumCover> AlbumCovers { get; set; } = new ObservableCollection<AlbumCover>();
    public override bool ContainsNestedRegions => false;
    public double DownloadedProcessValue { get; set; }
    public int FoundConvers { get; set; }
    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;
    public AlbumCover SelectedCover { get; set; }

    #endregion Properties

    #region Commands

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

    private async void OnSelectCover(AlbumCover albumCover)
    {
      SelectedCover = albumCover;
      SaveImage(SelectedCover);
    }


    #endregion SelectCover

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      path = albumViewModel.Model.AlbumFrontCoverFilePath;

      cancellationTokenSource = new CancellationTokenSource();
      var cancellationToken = cancellationTokenSource.Token;

      audioInfoDownloader.CoversDownloaded += Instance_CoversDownloaded;

      audioInfoDownloader.GetAlbumFrontCoversUrls(albumViewModel.Model, cancellationToken);
    }

    #endregion

    #region SaveImage

    private void SaveImage(AlbumCover albumCover)
    {
      MemoryStream ms = new MemoryStream(albumCover.DownloadedCover);
      Image i = Image.FromStream(ms);

      var directory = Path.Combine(AudioInfoDownloader.GetDefaultPicturesPath(), $"Albums\\{albumViewModel.Model.Id}");
      var finalPath = Path.Combine(directory, "frontConver.jpg");

      finalPath.EnsureDirectoryExists();

      if (File.Exists(finalPath))
        File.Delete(finalPath);

      i.Save(finalPath, ImageFormat.Jpeg);

      CacheImageConverter.RefreshDictionary(finalPath);

      albumViewModel.RaisePropertyChange(nameof(AlbumViewModel.ImageThumbnail));
      albumViewModel.RaisePropertyChange(nameof(AlbumViewModel.Image));

      storage.PushAction(new VCore.Modularity.Events.ItemChanged()
      {
        Changed = VCore.Modularity.Events.Changed.Updated,
        Item = albumViewModel.Model
      });
    }

    #endregion

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
    private Semaphore semaphore = new Semaphore(5, 5);
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
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                      cover.DownloadedCover = downloadedCover;
                      cover.Size = downloadedCover.Length;

                      AlbumCovers.Add(cover);

                      DownloadedProcessValue = (double)(AlbumCovers.Count * 100) / FoundConvers;
                    });
                  }

                  semaphore.Release();
                }
              }
              catch (WebException ex) when (ex.Status == WebExceptionStatus.RequestCanceled)
              {
                logger.Log(Logger.MessageType.Inform, "Downloading was stopped");
              }
              catch (Exception ex)
              {
                logger.Log(ex);
              }
            }
          }, cancellationTokenSource.Token);
        };
      }
      catch (WebException ex) when (ex.Status == WebExceptionStatus.RequestCanceled)
      {
        logger.Log(Logger.MessageType.Inform, "Downloading was stopped");
      }
    }


    #endregion Instance_CoversDownloaded

    #region OnBackCommand

    protected override void OnBackCommand()
    {
      base.OnBackCommand();

      cancellationTokenSource.Cancel();
    }

    #endregion

    #region GetRegionName

    public static string GetRegionName()
    {
      return RegionNames.HomeContentRegion;
    }
    #endregion

    #endregion
  }
}