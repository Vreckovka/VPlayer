using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VPlayer.AudioInfoDownloader;
using VPlayer.AudioStorage.AudioInfoDownloader.Models;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
  public class AlbumCoversViewModel : RegionViewModel<AlbumCoversView>
  {
    public ObservableCollection<AlbumCover> AlbumCovers { get; set; } = new ObservableCollection<AlbumCover>();
    public double DownloadedProcessValue { get; set; }
    public int FoundConvers { get; set; }

    public AlbumCoversViewModel(
        IRegionProvider regionProvider,
        AudioInfoDownloaderProvider audioInfoDownloaderProvider,
        AlbumViewModel albumViewModel) : base(regionProvider)
    {

      Task.Run(async () => { await audioInfoDownloaderProvider.GetAlbumFrontCoversUrls(albumViewModel.Model); });


      audioInfoDownloaderProvider.CoversDownloaded += Instance_CoversDownloaded;
    }

    public override string RegionName => RegionNames.LibraryContentRegion;
    public override bool ContainsNestedRegions => false;

    private object button = new object();
    private async void Instance_CoversDownloaded(object sender, List<AlbumCover> e)
    {
      FoundConvers += e.Count;

      foreach (var cover in e)
      {
        var downloadedCover = await Task.Run(() =>
        {
          using (var client = new WebClient())
          {
            return client.DownloadData(cover.Url);
          }
        });

        Application.Current.Dispatcher.Invoke(() =>
        {
          cover.DownloadedCover = downloadedCover;
          cover.Size = downloadedCover.Length;
          AlbumCovers.Add(cover);
          DownloadedProcessValue = (double)(AlbumCovers.Count * 100) / FoundConvers;
        });
      }
    }


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
  }
}
