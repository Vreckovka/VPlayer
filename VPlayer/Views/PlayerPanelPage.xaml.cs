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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VPlayer.Views
{
    /// <summary>
    /// Interaction logic for PlayerPanelPage.xaml
    /// </summary>
    public partial class PlayerPanelPage : Page
    {
        public PlayerPanelPage()
        {
            InitializeComponent();
            //PlayerHandler.Play += PlayerHandler_Play;
            //PlayerHandler.Pause += PlayerHandler_Pause; 
           
        }

        private void PlayerHandler_Pause(object sender, EventArgs e)
        {
            Play_Button.Tag = "Pause";
        }

        private void PlayerHandler_Play(object sender, EventArgs e)
        {
            Play_Button.Tag = "Play";
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            //if (Play_Button.Tag.Equals("Pause"))
            //{
            //    PlayerHandler.OnPlay(this);
            //}
            //else
            //{
            //    PlayerHandler.OnPause(this);
            //}
        }
    }
}
