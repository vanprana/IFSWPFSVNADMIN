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
using NLog;
using FirstFloor.ModernUI.Windows.Controls;
using IfsSvnClient.Classes;
using System.ComponentModel;

namespace IfsSvnClient.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlComponentManager.xaml
    /// </summary>
    public partial class UserControlComponentManager : UserControl
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private IfsSvn myIfsSvn;

        private BackgroundWorker backgroundWorkerCheckOut;
        private delegate void backgroundWorkerCheckOut_RunWorkerCompletedDelegate(object sender, RunWorkerCompletedEventArgs e);
        private bool _cancelCheckout = false;

        public UserControlComponentManager()
        {
            InitializeComponent();

            myIfsSvn = new IfsSvn();

            this.backgroundWorkerCheckOut = new BackgroundWorker();
            this.backgroundWorkerCheckOut.WorkerSupportsCancellation = true;
            this.backgroundWorkerCheckOut.DoWork += new DoWorkEventHandler(this.backgroundWorkerCheckOut_DoWork);
            this.backgroundWorkerCheckOut.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.backgroundWorkerCheckOut_RunWorkerCompleted);

        }

        private void buttonCreateComponets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ModernDialog.ShowMessage("Are you Sure?", "Just Checking", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    if (backgroundWorkerCheckOut.IsBusy == false)
                    {
                        progressBarMain.Visibility = System.Windows.Visibility.Visible;

                        backgroundWorkerCheckOut.RunWorkerAsync(new SvnManagerArguments(JobType.CreateComponents, textBoxCompornentNames.Text));
                    }
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Creating Components", MessageBoxButton.OK);
                logger.Error("Error Creating Components", ex);
            }
        }

        private void buttonCreateProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ModernDialog.ShowMessage("Are you Sure?", "Just Checking", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    if (backgroundWorkerCheckOut.IsBusy == false)
                    {
                        progressBarMain.Visibility = System.Windows.Visibility.Visible;

                        backgroundWorkerCheckOut.RunWorkerAsync(new SvnManagerArguments(JobType.CreateProject, textBoxProjectName.Text));
                    }
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Creating Components", MessageBoxButton.OK);
                logger.Error("Error Creating Components", ex);
            }
        }

        private void backgroundWorkerCheckOut_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Do not access the form's BackgroundWorker reference directly.
                // Instead, use the reference provided by the sender parameter.
                BackgroundWorker worker = sender as BackgroundWorker;

                if (worker.CancellationPending == false)
                {
                    SvnManagerArguments arg = e.Argument as SvnManagerArguments;

                    if (arg.Type == JobType.CreateProject)
                    {
                        e.Result = myIfsSvn.CreateProject(arg.ProjectName);
                    }
                    else if (arg.Type == JobType.CreateComponents)
                    {
                        e.Result = myIfsSvn.CreateComponents(arg.ComponentList);
                    }
                }

                // If the operation was canceled by the user, 
                // set the DoWorkEventArgs.Cancel property to true.
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                }
            }
            catch (Exception)
            {
                //throw the exception so that RunWorkerCompleted can catch it.
                throw;
            }
        }

        private void backgroundWorkerCheckOut_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (e.Error != null)
                {
                    ModernDialog.ShowMessage(e.Error.Message, "Error Creating Components", MessageBoxButton.OK);
                    logger.Error("Error Creating Components", e.Error);
                }
                else
                {
                    if (e.Result != null)
                    {
                        Mouse.OverrideCursor = null;
                        if ((bool)e.Result)
                        {
                            ModernDialog.ShowMessage("Done", "Created", MessageBoxButton.OK);
                        }
                        else
                        {
                            ModernDialog.ShowMessage("Issue", "Was Not Created", MessageBoxButton.OK);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error backgroundWorker issue", MessageBoxButton.OK);
            }
            finally
            {
                progressBarMain.Visibility = System.Windows.Visibility.Hidden;
                this._cancelCheckout = false;
                Mouse.OverrideCursor = null;
            }
        }
                
    }
}
