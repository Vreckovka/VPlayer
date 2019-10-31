using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;

namespace VCore.Controls
{
  public class PathButton : ToggleButton
  {
    #region PathStyle

    public Style PathStyle
    {
      get { return (Style)GetValue(PathStyleProperty); }
      set { SetValue(PathStyleProperty, value); }
    }

    public static readonly DependencyProperty PathStyleProperty =
      DependencyProperty.Register(
        nameof(PathStyle),
        typeof(Style),
        typeof(PathButton),
        new PropertyMetadata(null));


    #endregion

    #region IsToggle

    public bool IsToggle
    {
      get { return (bool)GetValue(IsToggleProperty); }
      set { SetValue(IsToggleProperty, value); }
    }

    public static readonly DependencyProperty IsToggleProperty =
      DependencyProperty.Register(
        nameof(IsToggle),
        typeof(bool),
        typeof(PathButton),
        new PropertyMetadata(false));


    #endregion

    #region OnToggle

    protected override void OnToggle()
    {
      if (IsToggle)
        base.OnToggle();
    }

    #endregion
  }
}
