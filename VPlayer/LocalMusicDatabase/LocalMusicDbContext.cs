using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPlayer.LocalDatabase;
using VPlayer.Migrations;

namespace VPlayer.LocalMusicDatabase
{
    public class LocalMusicDbContext : DbContext
    {
        public DbSet<Album> Albums { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Song> Songs { get; set; }

        public LocalMusicDbContext() : base()
        {

            var databaseLocation = Environment.CurrentDirectory + "\\LocalMusicDb\\LocalMusicDb.mdf";

            Database.Connection.ConnectionString =
                "Data Source=(LocalDB)\\MSSQLLocalDB;" +
                $"AttachDbFilename={databaseLocation};" +
                "Integrated Security=True;" +
                "MultipleActiveResultSets=true";

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<LocalMusicDbContext, Configuration>());
        }
    }
}
