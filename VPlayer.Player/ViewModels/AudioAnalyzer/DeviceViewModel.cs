using VCore.ViewModels;

namespace VPlayer.Player.ViewModels.AudioAnalyzer
{
  public class DeviceViewModel : ViewModel
  {
    public string Name { get; set; }
    public int BassWasapiIndex { get; set; }
  }
}