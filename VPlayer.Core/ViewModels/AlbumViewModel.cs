using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Prism.Events;
using VCore.Annotations;
using VCore.Factories;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.Modularity.Regions;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
    public class AlbumViewModel : PlayableViewModel<Album>
    {
        private readonly IStorageManager storage;
        private readonly IViewModelsFactory viewModelsFactory;
        private readonly IVPlayerRegionManager vPlayerRegionManager;

        public AlbumViewModel(
          Album model, IEventAggregator eventAggregator, 
            [NotNull] IStorageManager storage, [VCore.Annotations.NotNull]
          IViewModelsFactory viewModelsFactory,
          [NotNull] IVPlayerRegionManager vPlayerRegionManager) : base(model, eventAggregator)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
            this.vPlayerRegionManager = vPlayerRegionManager ?? throw new ArgumentNullException(nameof(vPlayerRegionManager));
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
          vPlayerRegionManager.ShowAlbumDetail(this);
       
        }
    }
}
