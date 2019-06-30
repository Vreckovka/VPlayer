using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPlayer.AudioStorage.Migrations;
using VPlayer.AudioStorage.Models;

namespace VPlayer.AudioStorage.AudioDatabase
{
    public class AudioDatabaseContext : DbContext
    {
        public DbSet<Album> Albums { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Song> Songs { get; set; }

        public AudioDatabaseContext()
        {
            Database.Connection.ConnectionString =
                "Data Source=(LocalDB)\\MSSQLLocalDB;" +
                $"AttachDbFilename={"C:\\Users\\Roman Pecho\\source\\repos\\VPlayer\\LocalMusicDb\\VPlayer.AudioDatabase.mdf"};" +
                "Integrated Security=True;" +
                "MultipleActiveResultSets=true";

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AudioDatabaseContext, Configuration>());
        }
    }
}
