using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace VPlayer.Home.Views
{
  // Each group owns a bounded viewport. The outer list retains the single selection
  // and ordered item collection used for keyboard movement across group boundaries.
  public class GroupedPlaylistListView : ListView
  {
    public static readonly DependencyProperty GroupOwnerProperty = DependencyProperty.Register(
      nameof(GroupOwner), typeof(ListView), typeof(GroupedPlaylistListView),
      new PropertyMetadata(null, OnOwnerChanged));

    private ListView subscribedOwner;

    public GroupedPlaylistListView()
    {
      Loaded += (sender, args) => Subscribe();
      Unloaded += (sender, args) => Unsubscribe();
    }

    public ListView GroupOwner
    {
      get => (ListView)GetValue(GroupOwnerProperty);
      set => SetValue(GroupOwnerProperty, value);
    }

    private static void OnOwnerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
      var list = (GroupedPlaylistListView)sender;
      list.Unsubscribe();
      list.Subscribe();
    }

    private void Subscribe()
    {
      if (subscribedOwner != GroupOwner)
      {
        Unsubscribe();
        subscribedOwner = GroupOwner;
        if (subscribedOwner != null) subscribedOwner.SelectionChanged += OnOwnerSelectionChanged;
      }
      SynchronizeSelection();
    }

    private void Unsubscribe()
    {
      if (subscribedOwner != null) subscribedOwner.SelectionChanged -= OnOwnerSelectionChanged;
      subscribedOwner = null;
    }

    private void OnOwnerSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
      if (ReferenceEquals(args.OriginalSource, subscribedOwner)) SynchronizeSelection();
    }

    private void SynchronizeSelection()
    {
      if (subscribedOwner == null) return;
      var selected = subscribedOwner.SelectedItem;
      SelectedItem = selected != null && Items.Contains(selected) ? selected : null;
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs args)
    {
      base.OnSelectionChanged(args);
      if (subscribedOwner == null) return;
      if (args.AddedItems.Count > 0 && !ReferenceEquals(subscribedOwner.SelectedItem, SelectedItem))
        subscribedOwner.SelectedItem = SelectedItem;
      else if (SelectedItem == null && args.RemovedItems.Contains(subscribedOwner.SelectedItem))
        subscribedOwner.SelectedItem = null;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs args)
    {
      if (GroupOwner != null && GroupOwner.Items.Count > 0 && !(Keyboard.FocusedElement is TextBoxBase))
      {
        int index = GroupOwner.Items.IndexOf(SelectedItem);
        object target = null;
        if (args.Key == Key.Home) target = GroupOwner.Items[0];
        else if (args.Key == Key.End) target = GroupOwner.Items[GroupOwner.Items.Count - 1];
        else if (args.Key == Key.Down && SelectedIndex == Items.Count - 1 && index >= 0 && index + 1 < GroupOwner.Items.Count)
          target = GroupOwner.Items[index + 1];
        else if (args.Key == Key.Up && SelectedIndex == 0 && index > 0)
          target = GroupOwner.Items[index - 1];
        if (target != null)
        {
          var destination = FindGroup(GroupOwner, target);
          if (destination != null)
          {
            destination.SelectedItem = target;
            destination.ScrollIntoView(target);
            destination.Dispatcher.BeginInvoke(new Action(() =>
            {
              if (ReferenceEquals(destination.SelectedItem, target))
                (destination.ItemContainerGenerator.ContainerFromItem(target) as ListViewItem)?.Focus();
            }), DispatcherPriority.ContextIdle);
            args.Handled = true;
          }
        }
      }
      base.OnPreviewKeyDown(args);
    }

    private static GroupedPlaylistListView FindGroup(DependencyObject root, object item)
    {
      if (root is GroupedPlaylistListView list && list.Items.Contains(item)) return list;
      for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
      {
        var found = FindGroup(VisualTreeHelper.GetChild(root, i), item);
        if (found != null) return found;
      }
      return null;
    }
  }
}
