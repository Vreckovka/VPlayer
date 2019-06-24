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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PropertyChanged;
using VPlayer.AudioStorage.Models;
using VPlayer.Library.ViewModels;

namespace VPlayer.Library.Views
{
    /// <summary>
    /// Interaction logic for AlbumsView.xaml
    /// </summary>


    public partial class AlbumsView : Page
    {
        public AlbumsView()
        {
            InitializeComponent();
            DataContext = LibraryViewModel.Instance;
        }

        private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var asd = LibraryViewModel.Instance.Albums;
        }

    }

    [ContentProperty("Actions")]
    public class ConditionalEventTrigger : FrameworkContentElement
    {
        public RoutedEvent RoutedEvent { get; set; }
        public List<TriggerAction> Actions { get; set; }

        // Condition
        public bool Condition { get { return (bool)GetValue(ConditionProperty); } set { SetValue(ConditionProperty, value); } }
        public static readonly DependencyProperty ConditionProperty = DependencyProperty.Register("Condition", typeof(bool), typeof(ConditionalEventTrigger));

        // "Triggers" attached property
        public static ConditionalEventTriggerCollection GetTriggers(DependencyObject obj) { return (ConditionalEventTriggerCollection)obj.GetValue(TriggersProperty); }
        public static void SetTriggers(DependencyObject obj, ConditionalEventTriggerCollection value) { obj.SetValue(TriggersProperty, value); }
        public static readonly DependencyProperty TriggersProperty = DependencyProperty.RegisterAttached("Triggers", typeof(ConditionalEventTriggerCollection), typeof(ConditionalEventTrigger), new PropertyMetadata
        {
            PropertyChangedCallback = (obj, e) =>
            {
                // When "Triggers" is set, register handlers for each trigger in the list 
                var element = (FrameworkElement)obj;
                var triggers = (List<ConditionalEventTrigger>)e.NewValue;
                foreach (var trigger in triggers)
                    element.AddHandler(trigger.RoutedEvent, new RoutedEventHandler((obj2, e2) =>
                      trigger.OnRoutedEvent(element)));
            }
        });

        public ConditionalEventTrigger()
        {
            Actions = new List<TriggerAction>();
        }

        // When an event fires, check the condition and if it is true fire the actions 
        void OnRoutedEvent(FrameworkElement element)
        {
            DataContext = element.DataContext;  // Allow data binding to access element properties
            if (Condition)
            {
                // Construct an EventTrigger containing the actions, then trigger it 
                var dummyTrigger = new EventTrigger { RoutedEvent = _triggerActionsEvent };
                foreach (var action in Actions)
                    dummyTrigger.Actions.Add(action);

                element.Triggers.Add(dummyTrigger);
                try
                {
                    element.RaiseEvent(new RoutedEventArgs(_triggerActionsEvent));
                }
                finally
                {
                    element.Triggers.Remove(dummyTrigger);
                }
            }
        }

        static RoutedEvent _triggerActionsEvent = EventManager.RegisterRoutedEvent("", RoutingStrategy.Direct, typeof(EventHandler), typeof(ConditionalEventTrigger));

    }

    // Create collection type visible to XAML - since it is attached we cannot construct it in code 
    public class ConditionalEventTriggerCollection : List<ConditionalEventTrigger> { }
}

