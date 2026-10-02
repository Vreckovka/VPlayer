using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.Core.FileBrowser;
using VPlayer.TestSupport;

namespace VPlayer.Performance
{
  internal static class FileBrowserBenchmarks
  {
    internal static void Prepare(string databaseDirectory,string directory)
    {
      directory=Path.GetFullPath(directory);
      if(Directory.Exists(directory))throw new IOException("Use a new disposable directory.");
      Directory.CreateDirectory(directory);
      using var context=new FixtureContext(Path.Combine(databaseDirectory,"VPlayerDatabase.db"));
      var names=context.FileInfos.AsNoTracking().Select(x=>x.Name).Where(x=>x!=null).Take(1000).ToArray();
      if(names.Length==0)throw new InvalidOperationException("Copied library contains no file names.");
      var invalid=Path.GetInvalidFileNameChars();
      names=names.Select(x=>new string(Path.GetFileNameWithoutExtension(x).Where(c=>!invalid.Contains(c)).Take(60).ToArray())).ToArray();
      int count=0;
      void Populate(string path,int files)
      {
        Directory.CreateDirectory(path);
        for(int i=0;i<files;i++)
        {
          using var file=File.Create(Path.Combine(path,$"copy-{count:D6}-{names[count%names.Length]}.txt"));count++;
        }
      }
      Populate(directory,1000);
      for(int i=0;i<100;i++)Populate(Path.Combine(directory,$"album-{i:D3}-library"),1000);
      var deep=directory;
      for(int i=0;i<32;i++){deep=Path.Combine(deep,"d"+i);Populate(deep,1);}
      Populate(Path.Combine(deep,"needle-folder"),1);
      File.WriteAllText(Path.Combine(directory,"fixture.json"),JsonSerializer.Serialize(new {
        Schema="file-browser-tree-v1",Files=count,Folders=133,Depth=33,LibraryNames=names.Length,
        MediaContentCopied=false,Description="Names copied from the disposable library; zero-byte files for directory/search tests."},new JsonSerializerOptions{WriteIndented=true}));
    }
    internal static void Run(string directory,string output,string commit)
    {
      if(commit.Length!=40 || commit.Any(c=>!Uri.IsHexDigit(c)))throw new ArgumentException("Exact source commit required.");
      if(File.Exists(output))throw new IOException("Use a new output file.");
      if(!File.Exists(Path.Combine(directory,"fixture.json")))throw new ArgumentException("Prepared fixture required.");
      using var fixture=new FileBrowserFixture(true);
      var results=new List<object>();
      var loading=Stopwatch.StartNew();fixture.Open(directory);loading.Stop();
      results.Add(new {Name="root-directory-load",Milliseconds=loading.Elapsed.TotalMilliseconds});
      int publications=0;
      fixture.Browser.RootFolder.SubItems.PropertyChanged+=(s,e)=>{if(e.PropertyName=="View")System.Threading.Interlocked.Increment(ref publications);};
      var errors=new List<string>();
      string[] Query(string query,string name)
      {
        int before=publications;
        var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;
        var watch=Stopwatch.StartNew();fixture.Browser.FilterPhrase=query;
        FileBrowserFixture.WaitUntil(()=>publications>before,TimeSpan.FromMinutes(5));watch.Stop();
        var root=fixture.Browser.RootFolder;
        var visible=root.SubItems.View.Select(x=>x.Name).ToArray();
        var displayed=fixture.Browser.Items.Generator.AllItems.Select(x=>x.Name).ToArray();
        results.Add(new {Name=name,Query=query,Milliseconds=watch.Elapsed.TotalMilliseconds,
          CpuMilliseconds=(process.TotalProcessorTime-cpu).TotalMilliseconds,
          RootMatches=visible.Length,DisplayedRootItems=displayed.Length,
          RootDisplayMatchesFilter=visible.SequenceEqual(displayed),
          VisibleFiles=CountVisible(root),RootNames=visible});
        Console.WriteLine(name+": "+watch.Elapsed.TotalMilliseconds.ToString("F0")+" ms");
        return VisiblePaths(root).ToArray();
      }
      Query("zzzz-no-such-track","first-recursive-miss");
      Query("yyyy-no-such-track","loaded-recursive-miss");
      var expectedPaths=Query("needle-folder","deep-folder-match");
      Query("NEEDLE-FOLDER","uppercase-deep-folder-match");
      Query("copy","wide-file-matches");
      fixture.Browser.FilterPhrase="";
      var rapidWatch=Stopwatch.StartNew();int rapidBefore=publications;
      foreach(var query in new[]{"zzzz-no-such-track","copy-0000","album-099","needle-folder"})fixture.Browser.FilterPhrase=query;
      // Baseline has no completion task. Each original request publishes once at the root.
      var completion=fixture.Browser.GetType().GetProperty("SearchCompletion");
      if(completion!=null)FileBrowserFixture.Await((System.Threading.Tasks.Task)completion.GetValue(fixture.Browser));
      else FileBrowserFixture.WaitUntil(()=>publications>=rapidBefore+4,TimeSpan.FromMinutes(5));
      rapidWatch.Stop();
      var rapidVisible=CountVisible(fixture.Browser.RootFolder);
      results.Add(new {Name="rapid-query-changes",Milliseconds=rapidWatch.Elapsed.TotalMilliseconds,
        VisibleFiles=rapidVisible,LatestQueryWon=VisiblePaths(fixture.Browser.RootFolder).SequenceEqual(expectedPaths)});
      fixture.Browser.FilterPhrase="yyyy-no-such-track";fixture.Browser.FilterPhrase="";
      if(completion!=null)FileBrowserFixture.Await((System.Threading.Tasks.Task)completion.GetValue(fixture.Browser));
      else FileBrowserFixture.WaitUntil(()=>publications>=rapidBefore+5,TimeSpan.FromMinutes(5));
      results.Add(new {Name="clear-in-flight",RootRestored=fixture.Browser.RootFolder.SubItems.View.Count==fixture.Browser.RootFolder.SubItems.ViewModels.Count});
      try{fixture.Browser.Filter(null);}catch(Exception ex){errors.Add("null-query: "+ex.GetType().Name);}
      using var loadingFixture=new FileBrowserFixture(true);
      loadingFixture.Open(directory);
      var loadCpu=Process.GetCurrentProcess().TotalProcessorTime;
      var loadAlloc=GC.GetAllocatedBytesForCurrentThread();
      var loadWatch=Stopwatch.StartNew();
      FileBrowserFixture.Await(loadingFixture.Browser.RootFolder.LoadSubFolders(loadingFixture.Browser.RootFolder));
      loadWatch.Stop();
      results.Add(new {Name="recursive-directory-load-only",Milliseconds=loadWatch.Elapsed.TotalMilliseconds,
        CpuMilliseconds=(Process.GetCurrentProcess().TotalProcessorTime-loadCpu).TotalMilliseconds,
        UiAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-loadAlloc,
        LoadedItems=loadingFixture.Browser.AllLoadedItems.Count()});
      File.WriteAllText(output,JsonSerializer.Serialize(new {Schema="file-browser-search-v2",Configuration=BenchmarkBuild.Configuration,SourceCommit=commit,
        Fixture=Path.GetFullPath(directory),Results=results,Errors=errors,FullPlayerUiMeasured=false,
        Boundary="Actual Windows browser and recursive folder view models; no rendered window or audio."},new JsonSerializerOptions{WriteIndented=true}));
    }
    private static IEnumerable<string> VisiblePaths(FolderViewModel<PlayableFileViewModel> root)
    {
      foreach(var item in root.SubItems.View)
      {
        if(item is FolderViewModel<PlayableFileViewModel> folder)
        {
          yield return folder.Model.Indentificator;
          foreach(var path in VisiblePaths(folder))yield return path;
        }
        else if(item is PlayableFileViewModel file)yield return file.Model.Indentificator;
      }
    }
    private static int CountVisible(FolderViewModel<PlayableFileViewModel> root)=>root.SubItems.View.Sum(x=>
      x is FolderViewModel<PlayableFileViewModel> folder?CountVisible(folder):1);
  }
}
