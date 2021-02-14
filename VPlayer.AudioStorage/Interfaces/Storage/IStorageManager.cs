using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using VCore.Modularity.Events;
using VPlayer.AudioStorage.DomainClasses;
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

    #endregion 

    #region Methods

    Task ClearStorage();
    Task DownloadAllNotYetDownloaded(bool tryDownloadBroken = false);
    IQueryable<T> GetRepository<T>(DbContext dbContext = null) where T : class;
    Task<bool> StoreData(IEnumerable<string> audioPath);
    Task<bool> StoreData(string audioPath);
    bool StorePlaylist<TPlaylist>(TPlaylist model, out TPlaylist entityModel) where TPlaylist : class, IPlaylist;
    void UpdateData<TPlaylist>(TPlaylist playlist) where TPlaylist : class, IPlaylist;
    void RewriteEntity<T>(T entity) where T : class, IEntity;
    Task<bool> UpdateEntity<TEntity>(TEntity newVersion) where TEntity : class, IEntity, IUpdateable<TEntity>;
    IDisposable SubscribeToItemChange<TModel>(Action<ItemChanged<TModel>> observer);
    Task DeletePlaylist(SongsPlaylist songsPlaylist);
    void PushAction(ItemChanged itemChanged);
    Task StoreTvShow(TvShow tvShow);

    #endregion Methods
  }
}