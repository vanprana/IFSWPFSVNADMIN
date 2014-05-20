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
using IfsSvnClient.Classes;
using FirstFloor.ModernUI.Windows.Controls;
using System.Deployment.Application;
using System.IO;
using NLog;

namespace IfsSvnClient.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlSettings.xaml
    /// </summary>
    public partial class UserControlSettings : UserControl
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

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
            catch (Exception ex)
            {
                logger.ErrorException("Error Resetting Property Settings", ex);
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
                logger.Info("buttonContactSupport: {0}", Properties.Settings.Default.SupportPerson);
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error contacting support", MessageBoxButton.OK);
                logger.ErrorException("Error contacting support", ex);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                myNotifierLync = new NotifierLync();

                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    Version ProductVersion = ApplicationDeployment.CurrentDeployment.CurrentVersion;

                    textBoxPublishVersion.Text = ProductVersion.ToString();
                }
            }
            catch (Exception)
            {
                buttonContactSupport.IsEnabled = false;
            }
        }

        private void button_BackUpLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "Please Select Root folder to copy logs";

                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    logger.Info("button_BackUpLog: {0}", dialog.SelectedPath);

                    string logDestinationFolderPath = dialog.SelectedPath + @"\logs";
                    string userExperienceDestinationFolderPath = dialog.SelectedPath + @"\userExperience";

                    if (Directory.Exists(logDestinationFolderPath) == false)
                    {
                        Directory.CreateDirectory(logDestinationFolderPath);
                    }
                    if (Directory.Exists(userExperienceDestinationFolderPath) == false)
                    {
                        Directory.CreateDirectory(userExperienceDestinationFolderPath);
                    }

                    string logSourceFolderPath = @".\logs";
                    string userExperienceSourceFolderPath = @".\userExperience";

                    if (Directory.Exists(logSourceFolderPath))
                    {
                        DirectoryInfo logsSourceFolder = new DirectoryInfo(logSourceFolderPath);
                        foreach (FileInfo logfile in logsSourceFolder.GetFiles())
                        {
                            logfile.CopyTo(logDestinationFolderPath + @"\" + logfile.Name);
                        }
                    }

                    if (Directory.Exists(userExperienceSourceFolderPath))
                    {
                        DirectoryInfo userExperienceSourceFolder = new DirectoryInfo(userExperienceSourceFolderPath);
                        foreach (FileInfo userExperiencefile in userExperienceSourceFolder.GetFiles())
                        {
                            userExperiencefile.CopyTo(userExperienceDestinationFolderPath + @"\" + userExperiencefile.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error backing up logs", MessageBoxButton.OK);
                logger.ErrorException("Error backing up logs", ex);
            }
        }
    }
}
