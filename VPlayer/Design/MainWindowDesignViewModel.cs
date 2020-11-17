using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.WindowsPlayer.Design;

namespace VPlayer.Design
{
  public class MainWindowDesignViewModel : BaseMainWindowViewModel
  {
    public MainWindowDesignViewModel()
    {
      NavigationViewModel.Items.Add(new NavigationItem(new WindowsPlayerDesignViewModel()));
    }

    public NavigationViewModel NavigationViewModel { get; set; } = new NavigationViewModel();
  }
}
