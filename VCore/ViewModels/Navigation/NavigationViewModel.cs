using System.Collections.ObjectModel;

namespace VCore.ViewModels.Navigation
{
  public class NavigationViewModel
  {
    public ObservableCollection<INavigationItem> Items { get; set; } = new ObservableCollection<INavigationItem>();


  }
}
