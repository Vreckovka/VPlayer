using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VCore.Standard.Modularity.Interfaces;

namespace VPlayer.WindowsPlayer.Views.Prompts
{
  /// <summary>
  /// Interaction logic for AddLyricsPromptView.xaml
  /// </summary>
  public partial class AddLyricsPromptView : UserControl, IView
  {
    public AddLyricsPromptView()
    {
      InitializeComponent();
    }
  }
}

