using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCore.Factories;
using VPlayer.AudioStorage.Models;
using VPlayer.Library.ViewModels.LibraryViewModels;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
    public class AlbumsViewModel : LibraryCollection<AlbumViewModel>
    {
        public AlbumsViewModel(IViewModelsFactory viewModelsFactory) : base(viewModelsFactory)
        {
        }

        public override void LoadData()
        {
            var storage = AudioStorage.StorageManager.GetStorage();
            {
                Items = storage.Albums.Select(x => ViewModelsFactory.Create<AlbumViewModel>(x));
            }
        }
    }
}
