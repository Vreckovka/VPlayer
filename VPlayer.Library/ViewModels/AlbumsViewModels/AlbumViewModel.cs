using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Prism.Events;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.AudioStorage.Models;
using VPlayer.Library.Annotations;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
    public class AlbumViewModel : PlayableViewModel<Album>
    {
        private readonly IStorage storage;

        public AlbumViewModel(Album model, IEventAggregator eventAggregator, [NotNull] IStorage storage) : base(model, eventAggregator)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public override string BottomText => Model.Songs.Count.ToString();
        public override byte[] ImageThumbnail => Model.AlbumFrontCoverBLOB;
        public override IEnumerable<Song> GetSongsToPlay()
        {
            return storage.Albums.Where(x => x.AlbumId == Model.AlbumId)
                .SelectMany(x => x.Songs);
        }
    }
}
