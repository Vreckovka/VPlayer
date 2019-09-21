using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Prism.Events;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.DomainClasses;
using VPlayer.Library.Properties;
using VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
    public class AlbumViewModel : PlayableViewModel<Album>
    {
        private readonly IStorageManager storage;

        public AlbumViewModel(Album model, IEventAggregator eventAggregator, [NotNull] IStorageManager storage) : base(model, eventAggregator)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }


        public override string BottomText => Model.Songs?.Count.ToString();
        public override byte[] ImageThumbnail => Model.AlbumFrontCoverBLOB;


        public override void Update(Album updateItem)
        {
            Model.AlbumFrontCoverBLOB = updateItem.AlbumFrontCoverBLOB;
        }

        public override IEnumerable<Song> GetSongsToPlay()
        {
            var songs = storage.GetRepository<Album>().Include(x => x.Songs).SelectMany(z => z.Songs).ToList();

            return songs;
        }
    }
}
