using OpenQA.Selenium.Chrome;

namespace VPlayer.AudioStorage.Scrappers.CSFD
{
  public interface IChromeDriverProvider
  {
    ChromeDriver ChromeDriver { get; set; }
    bool Initialize();
  }
}