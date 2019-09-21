using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PropertyChanged;
using VPlayer.AudioInfoDownloader.Models;
using VPlayer.AudioStorage;
using VPlayer.Core.DomainClasses;

namespace VPlayer.Library.Views
{
    /// <summary>
    /// Interaction logic for AvaibleCovers.xaml
    /// </summary>
    ///

    [AddINotifyPropertyChangedInterface]
    public partial class AlbumCoversView : Page
    {
        public ObservableCollection<Cover> Covers { get; set; } = new ObservableCollection<Cover>();

        public Album Album { get; set; }
        public AlbumCoversView(Album album)
        {
            InitializeComponent();
            DataContext = this;
            Album = album;

            //Task.Run(async () => { await AudioInfoDownloader.AudioInfoDownloaderProvider.Instance.GetAlbumFrontCoversUrls(album); });
           

            //AudioInfoDownloader.AudioInfoDownloaderProvider.Instance.CoversDownloaded += Instance_CoversDownloaded;
        }

        private async void Instance_CoversDownloaded(object sender, List<Cover> e)
        {
        await Dispatcher.InvokeAsync(async () =>
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
