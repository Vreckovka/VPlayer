using System.Diagnostics;
using System.Windows.Controls;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF.Managers;

namespace VPlayer.Player.Views
{
  /// <summary>
  /// Interaction logic for PlayerView.xaml
  /// </summary>
  public partial class PlayerView : UserControl, IView
  {
    public PlayerView()
    {
      InitializeComponent();
    }

    private void Popup_Opened(object sender, System.EventArgs e)
    {
      VFocusManager.SetFocus(WindowsSlider);
    }

  }
}
