using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VPlayer.AttachedProperties;
using VPlayer.AudioStorage.Models;
using VPlayer.Models;


namespace VPlayer.StylesDictionaries
{
    public partial class DataTemplates
    {
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            BaseEntity entity = (BaseEntity)((Button)sender).Tag;

            if (!entity.IsPlaying)
            {
                var storage = AudioStorage.StorageManager.GetStorage();
                foreach (var item in storage.Artists.Where(x => x.IsPlaying == true))
                {
                    item.IsPlaying = false;
                }

                foreach (var item in storage.Albums.Where(x => x.IsPlaying == true))
                {
                    item.IsPlaying = false;
                }

                entity.IsPlaying = true;

                if (entity is Artist)
                {
                    Task.Run(() =>
                    {
                        var song = ((Artist) entity).Albums.SelectMany(d => d.Songs);
                        PlayerHandler.PlayList = song.ToList();
                        PlayerHandler.Play();
                    });
                }
                    
            }
            else
            {
                entity.IsPlaying = false;
                PlayerHandler.Pause();
            }
              
        }
    }
}
