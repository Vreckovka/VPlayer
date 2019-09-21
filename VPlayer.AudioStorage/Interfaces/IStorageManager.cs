using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.Core.DomainClasses;

namespace VPlayer.AudioStorage.Interfaces
{
    /// <summary>
    /// Storage interface, automatic save after calling dispose
    /// </summary>
    public interface IStorageManager : IDisposable
    {
        Subject<ItemChanged> ItemChanged { get; } 

        Task StoreData(AudioInfo audioInfo);
        Task<bool> StoreData(IEnumerable<string> audioPath);

        Task<bool> StoreData(string audioPath);
        Task StoreData(List<AudioInfo> audioInfos);
        Task ClearStorage();

        Task UpdateItem(Artist artist);
        Task UpdateItem(Album album);
        Task UpdateAlbums(List<Album> albumsToUpdate);

        IQueryable<T> GetRepository<T>() where T : class;
    }
}
