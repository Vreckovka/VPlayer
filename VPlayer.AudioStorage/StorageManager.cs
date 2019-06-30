using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ninject;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.AudioStorage.Models;

namespace VPlayer.AudioStorage
{
    public static class StorageManager
    {
        public static event EventHandler<Album> AlbumStored;
        public static event EventHandler<Album> AlbumUpdated;
        public static event EventHandler<Album> AlbumRemoved;

        public static event EventHandler<Artist> ArtistStored;

        public static event EventHandler<EventArgs> StorageCleared;

        private static IKernel _kernel;

     
        static StorageManager()
        {
            _kernel = new StandardKernel();
            _kernel.Bind<IStorage>().To<AudioDatabaseManager>();
        }

        public static IStorage GetStorage()
        {
            return _kernel.Get<IStorage>();
        }

        internal static void OnAlbumStored(Album e)
        {
            AlbumStored?.Invoke(null, e);
        }

        internal static void OnAlbumUpdated(Album e)
        {
            AlbumUpdated?.Invoke(null, e);
        }

        internal static void OnAlbumRemoved(Album e)
        {
            AlbumRemoved?.Invoke(null, e);
        }

        internal static void OnStorageCleared()
        {
            StorageCleared?.Invoke(null, EventArgs.Empty);
        }

        public static void OnArtistStored(Artist e)
        {
            ArtistStored?.Invoke(null, e);
        }
    }
}