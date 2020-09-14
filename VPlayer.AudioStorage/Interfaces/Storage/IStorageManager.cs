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

    Subject<ItemChanged> ItemChanged { get; }
    Subject<Unit> ActionIsDone { get; }

    #endregion Properties

    #region Methods

    Task ClearStorage();
    Task UpdateAllNotYetUpdated(bool tryDownloadBroken = false);
    IQueryable<T> GetRepository<T>(DbContext dbContext = null) where T : class;
    Task<bool> StoreData(IEnumerable<string> audioPath);
    Task<bool> StoreData(string audioPath);
    bool StoreData(Playlist model, out Playlist entityModel);
    void UpdateData(Playlist playlist);
    void UpdateEntity<T>(T entity) where T : class, IEntity;

    

    #endregion Methods
  }
}