using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCore.Modularity.Events;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.DomainClasses.Video;

namespace VPlayer.AudioStorage.Interfaces.Storage
{
  /// <summary>
  /// Storage interface, automatic save after calling dispose
  /// </summary>
  public interface IStorageManager : IDisposable
  {
    #region Properties

    Subject<Unit> ActionIsDone { get; }

    ReplaySubject<ItemChanged> ItemChanged { get; }

    #endregion 

    #region Methods

    Task ClearStorage();
    Task DownloadAllNotYetDownloaded(bool tryDownloadBroken = false);
    DbSet<T> GetRepository<T>(DbContext dbContext = null) where T : class;

    #region Generic methods

    bool StoreEntity<TEntity>(TEntity model, out TEntity entityModel, bool log = true) where TEntity : class, IEntity;
    bool StoreRangeEntity<TEntity>(List<TEntity> entities , bool log = true) where TEntity : class, IEntity;

    bool DeleteEntity<TEntity>(TEntity entity) where TEntity : class, IEntity;
    Task<bool> UpdateEntityAsync<TEntity>(TEntity newVersion) where TEntity : class, IEntity, IUpdateable<TEntity>;
    void RewriteEntity<T>(T entity) where T : class, IEntity;

    Task DeletePlaylist<TPlaylist, TPlaylistItem>(TPlaylist songsPlaylist)
      where TPlaylist : class, IPlaylist<TPlaylistItem>
      where TPlaylistItem : class;

    bool UpdatePlaylist<TPlaylist, TPlaylistItem>(TPlaylist playlist, out TPlaylist updatedPlaylist) where TPlaylist : class, IPlaylist<TPlaylistItem> where TPlaylistItem: IEntity;

    #endregion

    Task<bool> StoreData(IEnumerable<string> audioPath);
    Task<bool> StoreData(string audioPath);
    Task<bool> DeleteTvChannelGroup(TvChannelGroup tvChannelGroup);



    IDisposable SubscribeToItemChange<TModel>(Action<ItemChanged<TModel>> observer);
    IObservable<ItemChanged<TModel>> ObserveOnItemChange<TModel>();

    Task<bool> UpdateWholeTvShow(TvShow newVersion);
    bool DeleteTvShow(TvShow tvShow);

    void PushAction(ItemChanged itemChanged);
    Task<int> StoreTvShow(TvShow tvShow);

    #endregion 
  }
}