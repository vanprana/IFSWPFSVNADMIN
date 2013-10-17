using System;
using System.Collections.Generic;
using System.Linq;
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
using IfsSvnAdmin.Classes;
using FirstFloor.ModernUI.Windows.Controls;

namespace IfsSvnAdmin.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlSettings.xaml
    /// </summary>
    public partial class UserControlSettings : UserControl
    {
        private NotifierLync myNotifierLync;

        public UserControlSettings()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Properties.Settings.Default.Reset();
            }
            catch (Exception)
            {

            }
        }

        private void buttonContactSupport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (myNotifierLync != null)
                {
                    MessageBoxResult contact = ModernDialog.ShowMessage(
                                                              "Do you really need to contact Me? :| ",
                                                              "Contact Support",
                                                              MessageBoxButton.YesNo);
                    if (contact == MessageBoxResult.Yes)
                    {
                        myNotifierLync.SendMessage(Properties.Settings.Default.HeaderMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error", MessageBoxButton.OK);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                myNotifierLync = new NotifierLync();
            }
            catch (Exception)
            {
                buttonContactSupport.IsEnabled = false;
            }
        }
    }
}
