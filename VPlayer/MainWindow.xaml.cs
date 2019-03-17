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
using VPlayer.LocalMusicDatabase;
using VPlayer.Views;

namespace VPlayer
{
    //TODO:Create pages
    //TODO: Library

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ListViewItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (((ListViewItem) sender).Content.Equals("Internet player"))
            {
                Frame_Players.Source = new Uri("Views/InternetPlayerPage.xaml", UriKind.Relative);
            }
            else
            {
                Frame_Players.Source = new Uri("Views/WindowsPlayerPage.xaml", UriKind.Relative);
            }
        }
    }
}
