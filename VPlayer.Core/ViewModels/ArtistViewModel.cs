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
using VPlayer.Library.ViewModels;

namespace VPlayer.Core.ViewModels
{
    public class ArtistViewModel : PlayableViewModel<Artist>
    {
        private readonly IStorageManager storage;
        private readonly IViewModelsFactory viewModelsFactory;
        private readonly IVPlayerRegionManager vPlayerRegionManager;

        public ArtistViewModel(Artist artist, IEventAggregator eventAggregator, IStorageManager storage,
            [NotNull] IViewModelsFactory viewModelsFactory, [NotNull] IVPlayerRegionManager vPlayerRegionManager) : base(artist, eventAggregator)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
            this.vPlayerRegionManager = vPlayerRegionManager ?? throw new ArgumentNullException(nameof(vPlayerRegionManager));
        }

        public override string BottomText => Model.Albums?.Count.ToString();
        public override byte[] ImageThumbnail => Model.ArtistCover;

        public override void Update(Artist updateItem)
        {
            Model.Albums = Model.Albums;
        }

        public override IEnumerable<Song> GetSongsToPlay()
        {
            var songs = storage.GetRepository<Artist>().Include(x => x.Albums.Select(y => y.Songs)).ToList();

            return songs.SelectMany(x => x.Albums.SelectMany(y => y.Songs));
        }

        protected override void OnDetail()
        {
          vPlayerRegionManager.ShowArtistDetail(this);
        }
    }

    public interface IPlayableViewModel<TModel> where TModel : INamedEntity
    {
        string Name { get; }
        int ModelId { get; }
        void Update(TModel updateItem);
    }
}