﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using VCore.Modularity.Events;
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
        IQueryable<T> GetRepository<T>(DbContext dbContext = null) where T : class;
        void UpdateEntity<T>(T entity) where T : class, IEntity;
    }
}