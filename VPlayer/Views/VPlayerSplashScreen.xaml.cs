using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VCore.Standard.Modularity.Interfaces;

namespace VPlayer.Views
{
  /// <summary>
  /// Interaction logic for VPlayerSplashScreen.xaml
  /// </summary>
  public partial class VPlayerSplashScreen : UserControl, IView
  {
    public VPlayerSplashScreen()
    {
      InitializeComponent();
    }
  }
}
