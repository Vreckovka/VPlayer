using System.ComponentModel;
using VCore.ViewModels.Navigation;

namespace VPlayer.WindowsPlayer.Design
{
  public class WindowsPlayerDesignViewModel : INavigationItem
  {
    #region Properties

    public string Header => "Windows player";
    public bool IsActive { get; set; }

    #endregion Properties

    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion Events

    public void Dispose()
    {
    }
  }
}