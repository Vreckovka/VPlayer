using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using VCore.Modularity.Events;
using VPlayer.AudioStorage.DomainClasses;

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
    bool StoreData(Playlist model, out Playlist entityModel);
    void UpdateData(Playlist playlist);
    void RewriteEntity<T>(T entity) where T : class, IEntity;
    Task<bool> UpdateEntity<TEntity>(TEntity newVersion) where TEntity : class, IEntity, IUpdateable<TEntity>;
    IDisposable SubscribeToItemChange<TModel>(Action<ItemChanged<TModel>> observer);
    Task DeletePlaylist(Playlist playlist);
    void PushAction(ItemChanged itemChanged);

    #endregion Methods
  }
}