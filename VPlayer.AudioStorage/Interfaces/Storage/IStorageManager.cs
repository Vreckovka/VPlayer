using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF.Modularity.Events;
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

    IObservable<IItemChanged> OnItemChanged { get; }

    #endregion

    #region Methods

    Task ClearStorage();
    Task DownloadAllNotYetDownloaded(bool tryDownloadBroken = false);
    DbSet<T> GetRepository<T>(DbContext dbContext = null) where T : class;

    #region Generic methods

    bool StoreEntity<TEntity>(TEntity model, out TEntity entityModel, bool log = true) where TEntity : class, IEntity;
    bool StoreAlbum(Album model, out Album entityModel, bool log = true);
    bool StoreRangeEntity<TEntity>(List<TEntity> entities, bool log = true) where TEntity : class, IEntity;

    bool DeleteEntity<TEntity>(TEntity entity) where TEntity : class, IEntity;
    Task<bool> UpdateEntityAsync<TEntity>(TEntity newVersion) where TEntity : class, IEntity, IUpdateable<TEntity>;
    Task<bool> UpdateEntitiesAsync<TEntity>(IEnumerable<TEntity> newVersions) where TEntity : class, IEntity, IUpdateable<TEntity>;
    void RewriteEntity<T>(T entity) where T : class, IEntity;
    Task<bool> UpdateSong(Song newVersion, bool updateAlbum = false, Album album = null);
    Task<bool> ResetSongs(IEnumerable<Song> newVersions);
    Task DeletePlaylist<TPlaylist, TPlaylistItem>(TPlaylist songsPlaylist)
      where TPlaylist : class, IPlaylist<TPlaylistItem>
      where TPlaylistItem : class;

    bool UpdatePlaylist<TPlaylist, TPlaylistItem, TModel>(TPlaylist playlist, out TPlaylist updatedPlaylist) where TPlaylist : class, IPlaylist<TPlaylistItem> 
      where TPlaylistItem : class, IItemInPlaylist<TModel> 
      where TModel : IEntity;

    #endregion

    Task<bool> StoreData(IEnumerable<string> audioPath);
    Task<bool> StoreData(string audioPath);
    Task<bool> DeleteTvChannelGroup(TvChannelGroup tvChannelGroup);


    void PublishItemChanged<TModel>(TModel model, Changed changed = Changed.Updated);
    IDisposable SubscribeToItemChange<TModel>(Action<IItemChanged<TModel>> observer);
    IObservable<IItemChanged<TModel>> ObserveOnItemChange<TModel>();

    Task<bool> DeepUpdateTvShow(TvShow newVersion);
    bool DeleteTvShow(TvShow tvShow);

    Task<int> StoreTvShow(TvShow tvShow);


    #endregion 
  }
}