using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VPlayer.AudioStorage.Models;

namespace VPlayer.AudioStorage.Interfaces
{
    /// <summary>
    /// Storage interface, automatic save after calling dispose
    /// </summary>
    public interface IStorage : IDisposable
    {
        IEnumerable<Album> Albums { get; }
        IEnumerable<Artist> Artists { get; }

        Task StoreData(AudioInfo audioInfo);
        Task StoreData(List<AudioInfo> audioInfos);
        Task ClearStorage();

        Task UpdateAlbum(Album album);
        Task UpdateAlbums(List<Album> albumsToUpdate);
    }
}
