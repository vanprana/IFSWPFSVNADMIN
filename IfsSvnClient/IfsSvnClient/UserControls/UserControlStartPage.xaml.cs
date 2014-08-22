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
using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Controls;

namespace IfsSvnClient.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlStartPage.xaml
    /// </summary>
    public partial class UserControlStartPage : UserControl, IContent
    {
        public UserControlStartPage()
        {
            InitializeComponent();
        }

        public void OnFragmentNavigation(FirstFloor.ModernUI.Windows.Navigation.FragmentNavigationEventArgs e)
        {

        }

        public void OnNavigatedFrom(FirstFloor.ModernUI.Windows.Navigation.NavigationEventArgs e)
        {

        }

        public void OnNavigatedTo(FirstFloor.ModernUI.Windows.Navigation.NavigationEventArgs e)
        {

        }

        public void OnNavigatingFrom(FirstFloor.ModernUI.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ServerUri))
                {
                    ModernDialog.ShowMessage("Please Select a Server.", "Server Select", MessageBoxButton.OK);
                    e.Cancel = true;
                }
            }
            catch (Exception)
            {

            }
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {                
                if (comboBoxServerUriList.SelectedValue != null)
                {
                    Properties.Settings.Default.ServerUri = comboBoxServerUriList.SelectedValue.ToString().Split(new char[] { '|' })[1].Trim();

                    BBCodeBlock bs = new BBCodeBlock();

                    bs.LinkNavigator.Navigate(new Uri("/UserControls/UserControlCheckOut.xaml", UriKind.Relative), this);
                }
                else 
                {
                    ModernDialog.ShowMessage("Please Select a Server.", "Server Select", MessageBoxButton.OK);
                }
            }
            catch (Exception error)
            {
                ModernDialog.ShowMessage(error.Message, FirstFloor.ModernUI.Resources.NavigationFailed, MessageBoxButton.OK);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ServerUri) == false)
                {
                    foreach (object item in comboBoxServerUriList.Items)
                    {
                        if (item.ToString().Contains(Properties.Settings.Default.ServerUri))
                        {
                            comboBoxServerUriList.SelectedValue = item;
                            break;
                        }
                    }
                }
            }
            catch (Exception error)
            {
                ModernDialog.ShowMessage(error.Message, "Error Loading", MessageBoxButton.OK);
            }
        }
    }
}
