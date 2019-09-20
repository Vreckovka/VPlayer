using System.ComponentModel;
using System.Runtime.CompilerServices;
using VCore.ViewModels.Navigation;
using VPlayer.WindowsPlayer.Annotations;

namespace VPlayer.WindowsPlayer.Design
{
  public class WindowsPlayerDesignViewModel : INavigationItem
  {
    public string Header => "Windows player";
    public event PropertyChangedEventHandler PropertyChanged;
    public bool IsActive { get; set; }
  }
}
