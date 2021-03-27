using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace VPlayer.WindowsPlayer.Behaviors
{
  public class FixListViewItemResizeBehavior : Behavior<ListView>
  {
    public double ValueDecrease { get; set; }

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
    }

    private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      for (int i = 0; i < AssociatedObject.Items.Count; i++)
      {
        var listViewItem = (ListViewItem)AssociatedObject.ItemContainerGenerator.ContainerFromIndex(i);

        if (listViewItem != null)
        {
          var newWidht = AssociatedObject.ActualWidth - ValueDecrease;

          if (newWidht >= 0)
          {
            listViewItem.Width = newWidht;
          }
        }
      }
    }

    protected override void OnDetaching()
    {
      AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;
    }
  }
}
