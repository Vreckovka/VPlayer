using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using VPlayer.TestSupport;
using Xunit;

namespace VPlayer.Tests
{
  [CollectionDefinition("File browser",DisableParallelization=true)]
  public class FileBrowserCollection {}
  [Collection("File browser")]
  public class FileBrowserSearchTests
  {
    [Fact]
    public void NullSearchBeforeOpeningADirectoryIsSafe()=>Sta.Run(()=>
    {
      using var fixture=new FileBrowserFixture();fixture.Browser.Filter(null);
    });
    [Fact]
    public void ClearingSearchBeforeOpeningADirectoryIsSafe()=>Sta.Run(()=>
    {
      using var fixture=new FileBrowserFixture();fixture.Browser.Filter("");
    });
    [Fact]
    public void FinderUpdatesTheTopLevelTreeAndClearRestoresIt()=>Sta.Run(()=>
    {
      var path=Path.Combine(@"D:\Aplikacie\VPlayer\artifacts\performance\file-browser-tests",Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(path);File.WriteAllText(Path.Combine(path,"alpha.txt"),"");File.WriteAllText(Path.Combine(path,"beta.txt"),"");
      using var fixture=new FileBrowserFixture();fixture.Open(path);
      int publications=0;
      fixture.Browser.RootFolder.SubItems.PropertyChanged+=(s,e)=>{if(e.PropertyName=="View")System.Threading.Interlocked.Increment(ref publications);};
      fixture.Browser.FilterPhrase="alpha";
      var completion=fixture.Browser.GetType().GetProperty("SearchCompletion");
      if(completion!=null)FileBrowserFixture.Await((Task)completion.GetValue(fixture.Browser));
      else FileBrowserFixture.WaitUntil(()=>publications>0,TimeSpan.FromSeconds(10));
      Assert.Equal(new[]{"alpha.txt"},fixture.Browser.Items.Generator.AllItems.Select(x=>x.Name));
      fixture.Browser.FilterPhrase="";
      if(completion!=null)FileBrowserFixture.Await((Task)completion.GetValue(fixture.Browser));
      Assert.Equal(new[]{"alpha.txt","beta.txt"},fixture.Browser.Items.Generator.AllItems.Select(x=>x.Name));
    });
  }
}
