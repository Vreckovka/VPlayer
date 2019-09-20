using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
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
      var directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      var path = Path.Combine(directory, "VPlayer.AudioDatabase.mdf");

      Database.Connection.ConnectionString =
                "Data Source=(LocalDB)\\MSSQLLocalDB;" +
                $"AttachDbFilename={path};" +
                "Integrated Security=True;" +
                "MultipleActiveResultSets=true";

      Database.SetInitializer(new MigrateDatabaseToLatestVersion<AudioDatabaseContext, Configuration>());
    }
  }
}
