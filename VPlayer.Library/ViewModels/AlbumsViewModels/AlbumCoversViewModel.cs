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
using VPlayer.AudioInfoDownloader.Models;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
  public class AlbumCoversViewModel : RegionViewModel<AlbumCoversView>
  {
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

    public ObservableCollection<Cover> Covers { get; set; } = new ObservableCollection<Cover>();

    private async void Instance_CoversDownloaded(object sender, List<Cover> e)
    {
      await Application.Current.Dispatcher.InvokeAsync(async () =>
      {
        foreach (var cover in e)
        {
          cover.Size = await Task.Run(() => { return GetFileSize(cover.Url); });
          Covers.Add(cover);
        }
      });
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
