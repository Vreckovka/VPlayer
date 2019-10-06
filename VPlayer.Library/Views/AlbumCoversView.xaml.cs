using PropertyChanged;
using System.Windows.Controls;
using VCore.Modularity.Interfaces;

namespace VPlayer.Library.Views
{
  /// <summary>
  /// Interaction logic for AvaibleCovers.xaml
  /// </summary>
  ///

  [AddINotifyPropertyChangedInterface]
  public partial class AlbumCoversView : UserControl, IView
  {
    #region Constructors

    public AlbumCoversView()
    {
      InitializeComponent();
    }

    #endregion Constructors
  }
}