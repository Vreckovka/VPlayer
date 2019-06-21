using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPlayer.AudioStorage.AudioDatabase.Migrations;
using VPlayer.AudioStorage.Models;

namespace VPlayer.AudioStorage
{
    public class AudioDatabaseContext : DbContext
    {
        public DbSet<Album> Albums { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Song> Songs { get; set; }

        public AudioDatabaseContext()
        {
            var solutionDirectory = TryGetSolutionDirectoryInfo();
            var databaseLocation = solutionDirectory.FullName + "\\LocalMusicDb\\LocalMusicDb.mdf";

            Database.Connection.ConnectionString =
                "Data Source=(LocalDB)\\MSSQLLocalDB;" +
                $"AttachDbFilename={databaseLocation};" +
                "Integrated Security=True;" +
                "MultipleActiveResultSets=true";


            //Database.SetInitializer<AudioDatabaseContext>(null);
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AudioDatabaseContext, Configuration>());
        }

        public static DirectoryInfo TryGetSolutionDirectoryInfo(string currentPath = null)
        {
            var directory = new DirectoryInfo(
                currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory;
        }

    }
}
