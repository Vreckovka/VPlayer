using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPlayer.AudioStorage.Models;
using VPlayer.Core.Factories;
using VPlayer.Library.ViewModels.ArtistsViewModels;
using VPlayer.Library.ViewModels.LibraryViewModels;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
    public class AlbumsViewModel : LibraryCollection
    {
        public AlbumsViewModel(IViewModelFactory viewModelFactory) : base(viewModelFactory)
        {
        }

        protected override void LoadData()
        {
            var storage = AudioStorage.StorageManager.GetStorage();
            {
                Items = storage.Albums.Select(x => ViewModelFactory.Create<AlbumViewModel>(x));
            }
        }
    }
}
