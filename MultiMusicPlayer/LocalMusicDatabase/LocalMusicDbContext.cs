using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiMusicPlayer.LocalDatabase;
using MultiMusicPlayer.Migrations;

namespace MultiMusicPlayer.LocalMusicDatabase
{
    public class LocalMusicDbContext : DbContext
    {
        public DbSet<Album> Albums { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Song> Songs { get; set; }

        public LocalMusicDbContext() : base()
        {
            var solutionDirectory = TryGetSolutionDirectoryInfo();

            if (solutionDirectory != null)
            {
                var databaseLocation = solutionDirectory.FullName + "\\LocalMusicDb\\LocalMusicDb.mdf";

                Database.Connection.ConnectionString =
                    "Data Source=(LocalDB)\\MSSQLLocalDB;" +
                    $"AttachDbFilename={databaseLocation};" +
                    "Integrated Security=True;" +
                    "MultipleActiveResultSets=true";

            }

             Database.SetInitializer(new MigrateDatabaseToLatestVersion<LocalMusicDbContext, Configuration>());
        }
        /// <summary>
        /// Fiding actual solution directory
        /// </summary>
        /// <param name="currentPath"></param>
        /// <returns></returns>
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
