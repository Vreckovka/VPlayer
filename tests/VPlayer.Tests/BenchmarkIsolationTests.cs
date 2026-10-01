using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using VPlayer.AudioStorage.AudioDatabase;
using Xunit;

namespace VPlayer.Tests
{
  [Collection("UI synchronization")]
  public class BenchmarkIsolationTests
  {
    [Fact]
    public void BenchmarkProfileUsesItsOwnDatabasePath()
    {
      var prior=Environment.GetEnvironmentVariable("VPLAYER_BENCHMARK_DIRECTORY");
      var root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"VPlayerProfile-"+Guid.NewGuid()));
      try
      {
        Environment.SetEnvironmentVariable("VPLAYER_BENCHMARK_DIRECTORY",root);
        using var context=new AudioDatabaseContext();
        Assert.Equal(Path.Combine(root,"VPlayerDatabase.db"),context.Database.GetDbConnection().DataSource);
        Assert.False(File.Exists(Path.Combine(root,"VPlayerDatabase.db")));
      }
      finally
      {
        Environment.SetEnvironmentVariable("VPLAYER_BENCHMARK_DIRECTORY",prior);
        if(!root.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
        if(Directory.Exists(root)) Directory.Delete(root,true);
      }
    }
  }
}
