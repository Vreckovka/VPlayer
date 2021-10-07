using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Logger;
using VCore;
using VCore.ItemsCollections.VirtualList;
using VCore.ItemsCollections.VirtualList.VirtualLists;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Helpers;
using VCore.ViewModels;
using VCore.WPF.Converters;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Models;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Managers.Status;
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
    private readonly IStatusManager statusManager;
    private readonly ILogger logger;
    private CancellationTokenSource cancellationTokenSource;
    private string tmpFolderPath;

    #endregion Fields

    #region Constructors

    public AlbumCoversViewModel(
      IRegionProvider regionProvider,
      AudioInfoDownloader audioInfoDownloader,
      AlbumViewModel albumViewModel,
      IStorageManager storage,
      IStatusManager statusManager,
      string regionName,
      ILogger logger) : base(regionProvider)
    {
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
      this.albumViewModel = albumViewModel ?? throw new ArgumentNullException(nameof(albumViewModel));
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

      RegionName = regionName;

      tmpFolderPath = Path.Combine(AudioInfoDownloader.GetDefaultPicturesPath() + "\\covers_tmp");
    }

    #endregion Constructors

    #region Properties

    public ObservableCollection<AlbumCoverViewModel> AlbumCovers { get; set; } = new ObservableCollection<AlbumCoverViewModel>();

    public override bool ContainsNestedRegions => false;
    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;

    #region View

    private VirtualList<AlbumCoverViewModel> view;

    public VirtualList<AlbumCoverViewModel> View
    {
      get { return view; }
      set
      {
        if (value != view)
        {
          view = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region DownloadedProcessValue

    private double downloadedProcessValue;

    public double DownloadedProcessValue
    {
      get { return downloadedProcessValue; }
      set
      {
        if (value != downloadedProcessValue)
        {
          downloadedProcessValue = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region FoundConvers

    private int foundConvers;

    public int FoundConvers
    {
      get { return foundConvers; }
      set
      {
        if (value != foundConvers)
        {
          foundConvers = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region SelectedCover

    private AlbumCoverViewModel selectedCover;

    public AlbumCoverViewModel SelectedCover
    {
      get { return selectedCover; }
      set
      {
        if (value != selectedCover)
        {
          selectedCover = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsDownloading

    private bool isDownloading;

    public bool IsDownloading
    {
      get { return isDownloading; }
      set
      {
        if (value != isDownloading)
        {
          isDownloading = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CoverUrlToDownload

    private string coverUrlToDownload;

    public string CoverUrlToDownload
    {
      get { return coverUrlToDownload; }
      set
      {
        if (value != coverUrlToDownload)
        {
          coverUrlToDownload = value;
          downloadFromUrl?.RaiseCanExecuteChanged();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion Properties

    #region Commands

    #region SelectCover

    private ActionCommand<AlbumCoverViewModel> selectCover;

    public ICommand SelectCover
    {
      get
      {
        if (selectCover == null)
        {
          selectCover = new ActionCommand<AlbumCoverViewModel>(OnSelectCover);
        }

        return selectCover;
      }
    }

    private async void OnSelectCover(AlbumCoverViewModel albumCover)
    {
      AlbumCovers.Where(x => x.IsSelected).ForEach(x => x.IsSelected = false);
      SelectedCover = null;

      SelectedCover = albumCover;
      SelectedCover.IsSelected = true;

      await SaveImage(SelectedCover);

      statusManager.UpdateMessage(new StatusMessageViewModel(1)
      {
        ProcessedCount = 1,
        Status = VCore.WPF.Controls.StatusMessage.StatusType.Done,
        Message = "Cover updated"
      });
    }


    #endregion

    #region Stop

    private ActionCommand stop;

    public ICommand Stop
    {
      get
      {
        if (stop == null)
        {
          stop = new ActionCommand(OnStop);
        }

        return stop;
      }
    }

    private void OnStop()
    {
      IsDownloading = false;
      cancellationTokenSource?.Cancel();
    }


    #endregion

    #region DownloadFromUrl

    private ActionCommand downloadFromUrl;

    public ICommand DownloadFromUrl
    {
      get
      {
        if (downloadFromUrl == null)
        {
          downloadFromUrl = new ActionCommand(OnDownloadFromUrl, () => { return !string.IsNullOrEmpty(CoverUrlToDownload); });
        }

        return downloadFromUrl;
      }
    }

    private async void OnDownloadFromUrl()
    {
      ResetViewModel();
      FoundConvers = 1;

      using (var webClient = new WebClient())
      {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource = new CancellationTokenSource();

        var result = await DowloadImage(webClient, new AlbumCover()
        {
          Url = CoverUrlToDownload
        });

        if (result)
        {
          DownloadedProcessValue = 100;
        }
      }
    }


    #endregion

    #region FindCovers

    private ActionCommand findCovers;

    public ICommand FindCovers
    {
      get
      {
        if (findCovers == null)
        {
          findCovers = new ActionCommand(OnFindCovers);
        }

        return findCovers;
      }
    }

    private async void OnFindCovers()
    {
      ResetViewModel();

      cancellationTokenSource?.Cancel();
      cancellationTokenSource = new CancellationTokenSource();

      var resul = await audioInfoDownloader.GetAlbumFrontCoversUrls(albumViewModel.Model, cancellationTokenSource.Token);

      if (resul == null)
      {
        FoundConvers = 0;
        DownloadedProcessValue = 0;
        cancellationTokenSource?.Cancel();
        IsDownloading = false;

        statusManager.UpdateMessage(new StatusMessageViewModel(1)
        {
          ProcessedCount = 1,
          Status = VCore.WPF.Controls.StatusMessage.StatusType.Failed,
          Message = $"Failed to download covers ARTIST (Model ID: {albumViewModel.Model?.Id} Name: {albumViewModel?.Model?.Name})"
        });
      }
    }


    #endregion 

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      audioInfoDownloader.CoversDownloaded += Instance_CoversDownloaded;
    }

    #endregion

    #region ResetViewModel

    private void ResetViewModel()
    {
      FoundConvers = 0;
      DownloadedProcessValue = 0;
      AlbumCovers.Clear();
      RequestReloadVirtulizedPlaylist();
      IsDownloading = true;
      ClearTmpFolder();
    }

    #endregion

    #region ClearTmpFolder


    private void ClearTmpFolder()
    {
      if (Directory.Exists(tmpFolderPath))
      {
        Directory.Delete(tmpFolderPath, true);
      }
    }

    #endregion

    #region SaveCover

    private Task<string> SaveCover(byte[] data)
    {
      return Task.Run(() =>
      {
        MemoryStream ms = new MemoryStream(data);
        Image i = Image.FromStream(ms);

        var finalPath = tmpFolderPath + "\\" + Guid.NewGuid() + ".jpg";

        finalPath.EnsureDirectoryExists();

        if (File.Exists(finalPath))
          File.Delete(finalPath);

        i.Save(finalPath, ImageFormat.Jpeg);

        ms?.Dispose();
        i?.Dispose();

        return finalPath;
      });
    }

    #endregion

    #region SaveImage

    private Task<string> SaveImage(AlbumCoverViewModel albumCover)
    {
      return Task.Run(() =>
      {
        MemoryStream ms = new MemoryStream(File.ReadAllBytes(albumCover.Model.DownloadedCoverPath));
        Image i = Image.FromStream(ms);

        var finalPath = albumViewModel.Model.AlbumFrontCoverFilePath;

        if (string.IsNullOrEmpty(finalPath))
        {
          finalPath = Path.Combine(AudioInfoDownloader.GetAlbumCoverImagePath(albumViewModel.Model));
        
          albumViewModel.Model.AlbumFrontCoverFilePath = finalPath;
        }

        finalPath.EnsureDirectoryExists();

        if (File.Exists(finalPath))
        {
          File.Delete(finalPath);
        }

        i.Save(finalPath, ImageFormat.Jpeg);
        CacheImageConverter.RefreshDictionary(finalPath);

        albumViewModel.Model.AlbumFrontCoverURI = albumCover.Model.Url;
        storage.UpdateEntityAsync(albumViewModel.Model);

        
        return finalPath;
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


    private SemaphoreSlim semaphore = new SemaphoreSlim(5, 1000);

    private void Instance_CoversDownloaded(object sender, List<AlbumCover> e)
    {
      Application.Current?.Dispatcher?.Invoke(() =>
      {
        FoundConvers += e.Count;
      });

      Task.Run(async () =>
      {
        foreach (var cover in e)
        {

          await semaphore.WaitAsync();
          try
          {
            using (var client = new WebClient())
            {
              var result = await DowloadImage(client, cover);

              if (result)
              {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                  DownloadedProcessValue = (double)(AlbumCovers.Count * 100) / FoundConvers;
                });
              }
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
          finally
          {

            semaphore.Release();
          }
        }
      });
    }


    #endregion

    #region DowloadImage

    private async Task<bool> DowloadImage(WebClient client, AlbumCover cover)
    {
      using (var registration = cancellationTokenSource.Token.Register(client.CancelAsync))
      {
        if (cancellationTokenSource.IsCancellationRequested)
          return false;

        var downloadedCover = await client.DownloadDataTaskAsync(cover.Url);

        if (downloadedCover != null)
        {
          var path = await SaveCover(downloadedCover);

          Application.Current?.Dispatcher?.Invoke(() =>
          {
            cover.Size = downloadedCover.Length;
            cover.DownloadedCoverPath = path;

            var vm = new AlbumCoverViewModel(cover);

            AlbumCovers.Add(vm);
            RequestReloadVirtulizedPlaylist();

            if (albumViewModel?.Model?.AlbumFrontCoverURI == cover.Url && !string.IsNullOrEmpty(cover.Url))
            {
              vm.IsSelected = true;
            }
          });
        }

        return downloadedCover != null;
      }
    }

    #endregion

    #region OnBackCommand

    protected override void OnBackCommand()
    {
      base.OnBackCommand();

      cancellationTokenSource?.Cancel();
    }

    #endregion

    #region GetRegionName

    public static string GetRegionName()
    {
      return RegionNames.HomeContentRegion;
    }
    #endregion

    #region RequestReloadVirtulizedPlaylist

    private Stopwatch stopwatchReloadVirtulizedPlaylist;
    private object batton = new object();
    private SerialDisposable serialDisposable = new SerialDisposable();

    protected void RequestReloadVirtulizedPlaylist()
    {
      int dueTime = 1500;

      if (AlbumCovers.Count > 50)
      {
        dueTime = 5000;
      }
     

      lock (batton)
      {
        serialDisposable.Disposable = Observable.Timer(TimeSpan.FromMilliseconds(dueTime)).Subscribe((x) =>
        {
          stopwatchReloadVirtulizedPlaylist = null;
          ReloadVirtulizedPlaylist();
        });

        if (stopwatchReloadVirtulizedPlaylist == null || stopwatchReloadVirtulizedPlaylist.ElapsedMilliseconds > dueTime)
        {
          ReloadVirtulizedPlaylist();

          serialDisposable.Disposable?.Dispose();
          stopwatchReloadVirtulizedPlaylist = new Stopwatch();
          stopwatchReloadVirtulizedPlaylist.Start();
        }
      }
    }

    #endregion

    #region ReloadVirtulizedPlaylist

    private void ReloadVirtulizedPlaylist()
    {
      var generator = new ItemsGenerator<AlbumCoverViewModel>(AlbumCovers.OrderByDescending(x => x.Model.Size), 15);

      Application.Current?.Dispatcher?.Invoke(() =>
      {
        View = new VirtualList<AlbumCoverViewModel>(generator);
      });

      GC.Collect();

    }

    #endregion


    public override void Dispose()
    {
      base.Dispose();

      ClearTmpFolder();
    }

    #endregion
  }
}