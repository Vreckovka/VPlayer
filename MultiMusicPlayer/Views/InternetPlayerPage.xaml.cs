using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using Gecko;
using Gecko.DOM;
using Gecko.WebIDL;
using MultiMusicPlayer.Models;
using MultiMusicPlayer.ViewModels;
using Console = System.Console;
using Grid = System.Windows.Controls.Grid;

namespace MultiMusicPlayer.Views
{
    /// <summary>
    /// Interaction logic for InternetPlayerPage.xaml
    /// </summary>
    ///

    public partial class InternetPlayerPage : Page
    {
        private InternetPlayersView _internetPlayersView;
        private GeckoWebBrowser browser;
        private bool _fidingState;
        private string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:64.0) Gecko/20100101 Firefox/64.0";
        private string extentionFolder = "C:\\Users\\Roman Pecho\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\dmuirs8g.default\\extensions";

        public class DOMEventListener : nsIDOMEventListener
        {
            public void HandleEvent(nsIDOMEvent @event)
            {
                Console.WriteLine("Click");
            }
        }

        private static DomEventTarget _playButton;
        private DomEventArgs _clickEvent;
        DOMEventListener _domEventListener = new DOMEventListener();
        public InternetPlayerPage()
        {
            InitializeComponent();
            _internetPlayersView = new InternetPlayersView();
            DataContext = _internetPlayersView;

            PlayerHandler.Play += PlayerHandler_Play;
            PlayerHandler.Pause += PlayerHandler_Play;
           
            Xpcom.Initialize("Firefox64");
        }
        
        private void PlayerHandler_Play(object sender, EventArgs e)
        {
            if (IsVisible)
            {
                try
                {
                    _playButton.DispatchEvent(_clickEvent);
                }
                catch (NullReferenceException ex)
                {
                    MessageBox.Show("Play button was not indentified yet, or was lost");
                }
            }
        }

        private void ListViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            browser.Navigate((((InternetPlayer)((ListViewItem)sender).Content).Uri).AbsoluteUri);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            WindowsFormsHost host = new WindowsFormsHost();
            browser = new GeckoWebBrowser();
            host.Child = browser;
            Grid.SetRow(host, 1);
            GRIDAS.Children.Add(host);

            browser.Navigate("https://www.detectadblock.com/");
            browser.Navigated += Browser_Navigated;
            Gecko.GeckoPreferences.User["general.useragent.override"] = _userAgent;
        }

        private void Browser_Navigated(object sender, GeckoNavigatedEventArgs e)
        {
            browser.DomClick += Browser_DomClick;
        }

        private void Browser_DomClick(object sender, DomMouseEventArgs e)
        {
            if (_fidingState)
            {
                _clickEvent = browser.Document.CreateEvent("MouseEvent");
                var webEvent = new Event((mozIDOMWindowProxy)browser.Window.DomWindow, _clickEvent.DomEvent as nsISupports);
                webEvent.InitEvent("click", true, true);

                _playButton = e.Target;
                e.Target.NativeObject.AddSystemEventListener(new nsAString("click"), _domEventListener, true, true, 10);
                DisableFidingState();
            }
        }

        private void EnableFindingState()
        {
            Button_Fiding.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#AD4dba5d")); ;
            Button_Fiding.Content = "Click on play button";
            _fidingState = true;
        }

        private void DisableFidingState()
        {
            Button_Fiding.Background = (Brush)FindResource("DefaultRedBrush");
            Button_Fiding.Content = "Click for finding play button";
            _fidingState = false;
        }

        private void FindPlay_Click(object sender, RoutedEventArgs e)
        {
            EnableFindingState();
        }

        private void GoToURL_Click(object sender, RoutedEventArgs e)
        {
            //var url = TextBox_URL.Text.Replace("https://", "").Replace("");

            //browser.Navigate();
        }
    }
}
