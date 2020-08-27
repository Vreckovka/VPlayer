using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VCore.Modularity.Interfaces;

namespace VPlayer.Core.Views
{
  /// <summary>
  /// Interaction logic for SettingsPage.xaml
  /// </summary>
  public partial class SettingsView : UserControl, IView
  {
    public SettingsView()
    {
      InitializeComponent();
    }
  }
}
