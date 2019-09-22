using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Prism.Events;
using VCore.Factories;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.DomainClasses;
using VPlayer.Library.Properties;
using VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
    public class AlbumViewModel : PlayableViewModel<Album>
    {
        private readonly IStorageManager storage;
        private readonly IViewModelsFactory viewModelsFactory;

        public AlbumViewModel(Album model, IEventAggregator eventAggregator, 
            [NotNull] IStorageManager storage, [VCore.Annotations.NotNull] IViewModelsFactory viewModelsFactory) : base(model, eventAggregator)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
        }


        public override string BottomText => Model.Songs?.Count.ToString();
        public override byte[] ImageThumbnail => Model.AlbumFrontCoverBLOB;


        public override void Update(Album updateItem)
        {
            Model.AlbumFrontCoverBLOB = updateItem.AlbumFrontCoverBLOB;
        }

        public override IEnumerable<Song> GetSongsToPlay()
        {
            var songs = storage.GetRepository<Song>()
                .Include(x => x.Album)
                .Where(x => x.Album.Id == Model.Id).ToList();


            return songs;
        }

        protected override void OnDetail()
        {
            var asd = viewModelsFactory.Create<AlbumDetailViewModel>(this);
            asd.IsActive = true;
        }
    }
}
