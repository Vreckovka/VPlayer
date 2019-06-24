using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualListWithPagingTechnique;
using VPlayer.AudioStorage;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.AudioStorage.Models;

namespace VPlayer.Library.VirtualList
{
    public class AlbumGenerator : IObjectGenerator<Album>
    {
        public AlbumGenerator()
        {
            using (IStorage storage = StorageManager.GetStorage())
            {
                Albums = storage.Albums.ToList();
            }
        }
        public List<Album> Albums { get; set; }
        public int Count => Albums.Count;
        public Album CreateObject(int index)
        {
            return Albums[index];
        }
    }
}
