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

namespace VPlayer.UPnP.Views
{
  /// <summary>
  /// Interaction logic for UPnPManagerView.xaml
  /// </summary>
  public partial class UPnPManagerView : UserControl, IView
  {
    public UPnPManagerView()
    {
      InitializeComponent();
    }
  }
}
