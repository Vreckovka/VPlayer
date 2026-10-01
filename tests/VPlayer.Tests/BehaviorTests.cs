using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;
using PCloudClient;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.LRC;
using VCore.WPF.LRC.Domain;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud;
using VPlayer.AudioStorage.Repositories;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Player.UserControls;
using VPlayer.WindowsPlayer;
using Xunit;

namespace VPlayer.Tests
{
  public class PlaylistTests
  {
    private class Row { public int Id; public int Track; public int Order; public bool Available = true; }
    [Fact]
    public void DuplicateTracksKeepTheirRowIdentityAndUnavailableSlots()
    {
      var rows = new[] {
        new Row { Id=1, Track=10, Order=1 },
        new Row { Id=2, Track=20, Order=2, Available=false },
        new Row { Id=3, Track=10, Order=3 },
        new Row { Id=4, Track=30, Order=4 }
      };
      var result = PlaylistOrder.ReorderAvailable(rows, new[] {10,30,10}, x=>x.Track, x=>x.Available);
      Assert.Equal(new[] {1,2,4,3}, result.Select(x=>x.Id));
      Assert.Equal(new[] {1,2,3,4}, rows.Select(x=>x.Id));
    }
    [Theory]
    [InlineData(new int[] {10})]
    [InlineData(new int[] {10,99})]
    [InlineData(new int[] {10,10,20})]
    public void StaleOrPartialReordersAreRejected(int[] ids)
    {
      var rows = new[] {new Row {Track=10}, new Row {Track=20}};
      Assert.Throws<InvalidOperationException>(()=>PlaylistOrder.ReorderAvailable(rows, ids, x=>x.Track, x=>x.Available));
    }
    [Fact]
    public void ChangeDetectionIgnoresEnumerationOrderButDetectsTrackReplacement()
    {
      var rows = new[] {new Row {Id=1, Track=10, Order=1}, new Row {Id=2, Track=20, Order=2}};
      Assert.False(PlaylistOrder.HasChanges(rows, Enumerable.Reverse(rows), x=>x.Id, x=>x.Track, x=>x.Order));
      Assert.True(PlaylistOrder.HasChanges(rows, new[] {rows[0], new Row {Id=2, Track=30, Order=2}}, x=>x.Id, x=>x.Track, x=>x.Order));
    }
  }

  public class FilenameTests
  {
    [Theory]
    [InlineData("Show S01E02.mkv", 1, 2)]
    [InlineData("Show S01xE02.mkv", 1, 2)]
    [InlineData("Show 1x02.mkv", 1, 2)]
    [InlineData("Show [1.02].mkv", 1, 2)]
    [InlineData("Show [1.02-03].mkv", 1, 2)]
    [InlineData("Show 1-2", 1, 2)]
    [InlineData("Show (E12).mkv", null, 12)]
    [InlineData("Show - 12.mkv", null, 12)]
    public void RecognizesDocumentedEpisodeFormats(string name, int? season, int episode)
    {
      var result = DataLoader.GetTvShowSeriesNumber(name);
      Assert.NotNull(result);
      Assert.Equal(season, result.SeasonNumber);
      Assert.Equal(episode, result.EpisodeNumber);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Movie [1a02].mkv")]
    [InlineData("Movie 1920x1080.mkv")]
    public void InvalidEpisodeNamesAreNotShows(string name) => Assert.Null(DataLoader.GetTvShowSeriesNumber(name));
    [Theory]
    [InlineData("Movie (2001).mkv", 2001)]
    [InlineData("Movie.2001.mkv", 2001)]
    [InlineData("Movie 2001", 2001)]
    public void RecognizesMovieYears(string name, int year) => Assert.Equal(year, DataLoader.GetYear(name).YearNumber);
  }

  public class RepositoryTests
  {
    public class Entity { public int Id { get; set; } }
    public class Context : DbContext
    {
      public static int Created;
      public Context() { Created++; }
      protected override void OnConfiguring(DbContextOptionsBuilder builder) => builder.UseSqlite("Data Source=:memory:");
      protected override void OnModelCreating(ModelBuilder builder) => builder.Entity<Entity>().HasKey(x=>x.Id);
    }
    private class Repository : GenericRepository<Context,Entity> { public Repository(Context context) : base(context) {} }
    [Fact]
    public void SuppliedContextIsUsedWithoutCreatingAnUnusedContext()
    {
      using var context = new Context();
      var before = Context.Created;
      var repository = new Repository(context);
      Assert.Same(context, repository.Context);
      Assert.Equal(before, Context.Created);
    }
    [Fact]
    public void AccessingEntitiesPreservesContextAndPendingChanges()
    {
      using var context = new Context();
      var repository = new Repository(context);
      var entity = new Entity {Id=1};
      repository.Add(entity);
      Assert.Same(context.Set<Entity>(), repository.Entities);
      Assert.Same(context, repository.Context);
      Assert.Equal(EntityState.Added, context.Entry(entity).State);
    }
  }

  public class LyricsTests
  {
    internal static LRCFileViewModel Create(params double[] seconds)
    {
      var lines = seconds.Select((x,i)=>new LRCLyricLine {Timestamp=TimeSpan.FromSeconds(x), Text="Line "+i}).ToList();
      var windows = new Mock<IWindowManager>().Object;
      var provider = new PCloudLyricsProvider(new Mock<IPCloudService>().Object, windows, new Mock<IPCloudProvider>().Object);
      return new LRCFileViewModel(new LRCFile(lines), LRCProviders.Local, provider, windows);
    }
    [Fact]
    public void SelectsLastEligibleTimestampWhenInputIsUnsorted()
    {
      var lyrics = Create(0,30,10,20);
      lyrics.SetActualLine(TimeSpan.FromSeconds(25));
      Assert.Equal(TimeSpan.FromSeconds(20), lyrics.ActualLine.Model.Timestamp);
    }
    [Fact]
    public void BackwardSeekBeforeFirstLineClearsOldHighlight()
    {
      var lyrics = Create(0,10,20);
      lyrics.SetActualLine(TimeSpan.FromSeconds(15));
      var old = lyrics.ActualLine;
      lyrics.SetActualLine(TimeSpan.FromSeconds(-1));
      Assert.Null(lyrics.ActualLine);
      Assert.False(old.IsActual);
    }
    [Fact]
    public void TimingAdjustmentImmediatelyChangesSelectionAtSamePlaybackTime()
    {
      var lyrics = Create(0,10,20);
      lyrics.SetActualLine(TimeSpan.FromSeconds(15));
      lyrics.TimeAdjustment = 10000;
      lyrics.SetActualLine(TimeSpan.FromSeconds(15));
      Assert.Equal(TimeSpan.Zero, lyrics.ActualLine.Model.Timestamp);
    }
    [Fact]
    public void RepeatedPlaybackTicksPublishOnlyLineTransitions()
    {
      var lyrics = Create(0,10,20);
      var transitions = new List<int>();
      using var subscription = lyrics.ActualLineChanged.Subscribe(transitions.Add);
      lyrics.SetActualLine(TimeSpan.FromSeconds(11));
      lyrics.SetActualLine(TimeSpan.FromSeconds(12));
      lyrics.SetActualLine(TimeSpan.FromSeconds(21));
      Assert.Equal(2, transitions.Count);
    }
  }

  public class SignalTests
  {
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1)]
    [InlineData(0)]
    public void InvalidOrSilentSignalsProduceZero(double input) => Assert.Equal(0, new AdaptiveSignalNormalizer().Update(input));
    [Fact]
    public void NormalizationStaysBoundedAndResetRestartsWarmup()
    {
      var normalizer = new AdaptiveSignalNormalizer();
      foreach(var input in new[] {0.0,0.01,0.5,1.0,100.0,0.0})
        Assert.InRange(normalizer.Update(input), 0, 1);
      normalizer.Reset();
      Assert.Equal(0, normalizer.Update(0.5));
    }
  }

  public class PlaybackConverterTests
  {
    [Fact]
    public void UnknownDurationProducesFiniteProgress()
    {
      var result = new ActualTimeConverter().Convert(new object[] {TimeSpan.Zero,TimeSpan.Zero}, typeof(double),null,CultureInfo.InvariantCulture);
      Assert.Equal(0.0, result);
    }
    [Fact]
    public void KnownDurationProducesPercentage()
    {
      var result = new ActualTimeConverter().Convert(new object[] {TimeSpan.FromSeconds(25),TimeSpan.FromSeconds(100)}, typeof(double),null,CultureInfo.InvariantCulture);
      Assert.Equal(25.0, result);
    }
  }
}


