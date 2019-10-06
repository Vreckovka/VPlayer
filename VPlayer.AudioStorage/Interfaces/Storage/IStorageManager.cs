using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
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

    #endregion Properties

    #region Methods

    Task ClearStorage();

    IQueryable<T> GetRepository<T>(DbContext dbContext = null) where T : class;

    Task StoreData(AudioInfo audioInfo);

    Task<bool> StoreData(IEnumerable<string> audioPath);

    Task<bool> StoreData(string audioPath);

    Task StoreData(List<AudioInfo> audioInfos);

    void UpdateEntity<T>(T entity) where T : class, IEntity;

    #endregion Methods
  }
}