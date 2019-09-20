using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.ViewModels;
using VPlayer.WindowsPlayer.Design;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.Design
{
  public class MainWindowDesignViewModel : BaseMainWindowViewModel
  {
    public MainWindowDesignViewModel()
    {
      NavigationViewModel.Items.Add(new WindowsPlayerDesignViewModel());
    }

    public NavigationViewModel NavigationViewModel { get; set; } = new NavigationViewModel();
  }
}
