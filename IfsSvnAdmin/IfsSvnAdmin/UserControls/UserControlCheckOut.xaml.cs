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
using System.ComponentModel;
using IfsSvnAdmin.Classes;
using SharpSvn;
using SharpSvn.UI;
using System.Collections.ObjectModel;

namespace IfsSvnAdmin.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlCheckOut.xaml
    /// </summary>
    public partial class UserControlCheckOut : UserControl
    {
        private BackgroundWorker backgroundWorkerCheckOut;
        private delegate void backgroundWorkerCheckOut_RunWorkerCompletedDelegate(object sender, RunWorkerCompletedEventArgs e);
        private bool _cancelCheckout = false;
        private readonly SvnUriTarget projectsUri = new SvnUriTarget(Properties.Settings.Default.ServerUri + "/projects");
        private delegate void client_NotifyDelegate(object sender, SvnNotifyEventArgs e);

        private BitmapImage bi;
        private IfsSvn myIfsSvn;

        public UserControlCheckOut()
        {
            InitializeComponent();

            myIfsSvn = new IfsSvn();

            this.backgroundWorkerCheckOut = new BackgroundWorker();
            this.backgroundWorkerCheckOut.WorkerSupportsCancellation = true;
            this.backgroundWorkerCheckOut.DoWork += new DoWorkEventHandler(this.backgroundWorkerCheckOut_DoWork);
            this.backgroundWorkerCheckOut.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.backgroundWorkerCheckOut_RunWorkerCompleted);
              
            bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(@"/IfsSvnAdmin;component/Resources/folder.png", UriKind.RelativeOrAbsolute);
            bi.EndInit();
        }

        private void buttonCheckOut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (buttonCheckOut.Content.ToString() == "Check Out")
                {
                    this.StartCheckOut();
                }
                else if (buttonCheckOut.Content.ToString() == "Cancel")
                {
                    this._cancelCheckout = true;
                }
            }
            catch (Exception ex)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show(App.Current.MainWindow,
                                  ex.Message,
                                  "Error Checking out Components",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartCheckOut()
        {
            try
            {                
                if (backgroundWorkerCheckOut.IsBusy == false)
                {
                    if (listBoxProjectList.SelectedItem != null &&
                        (listBoxProjectList.SelectedItem as ListBoxItem).Tag != null)
                    {
                        buttonCheckOut.Content = "Cancel";

                        string projectPath = ((listBoxProjectList.SelectedItem as ListBoxItem).Tag as SvnProject).Path;

                        SvnComponent[] compornentArray = new SvnComponent[listBoxComponents.SelectedItems.Count];
                        listBoxComponents.SelectedItems.CopyTo(compornentArray, 0);
                        Uri projectUri = new Uri(projectPath + Properties.Settings.Default.ServerWorkSpace + "/");

                        backgroundWorkerCheckOut.RunWorkerAsync(new CheckOutArguments(JobType.CheckOut,
                                                                                      projectUri,
                                                                                      textBoxWorkSpace.Text, 
                                                                                      compornentArray));
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void CheckOutProject(CheckOutArguments arg)
        {
            using (SvnClient client = new SvnClient())
            {
                // Bind the SharpSvn UI to our client for SSL certificate and credentials
                SvnUIBindArgs bindArgs = new SvnUIBindArgs();
                SvnUI.Bind(client, bindArgs);

                client.Notify += new EventHandler<SvnNotifyEventArgs>(client_Notify);
                client.Cancel += new EventHandler<SvnCancelEventArgs>(client_Cancel);

                Uri rootUri = client.GetRepositoryRoot(arg.ProjectUri);

                client.CheckOut(arg.ProjectUri, arg.CheckOutPath, new SvnCheckOutArgs() { IgnoreExternals = true });

                Uri componentUri;

                foreach (SvnComponent component in arg.CompornentArray)
                {
                    componentUri = new Uri(component.Path.Replace("^/", rootUri.AbsoluteUri));

                    client.CheckOut(componentUri, arg.CheckOutPath + @"\" + component.Name);
                }
            }
        }

        private void client_Notify(object sender, SvnNotifyEventArgs e)
        {
            try
            {
                if (textBoxLog.Dispatcher.CheckAccess())
                {
                    textBoxLog.AppendText(DateTime.Now.ToString() + " " + e.Action + " " + e.FullPath + "\r\n");
                    textBoxLog.ScrollToEnd();
                }
                else
                {
                    textBoxLog.Dispatcher.Invoke(new client_NotifyDelegate(client_Notify), new object[] { sender, e });
                }
            }
            catch (Exception)
            {

            }
        }

        private void client_Cancel(object sender, SvnCancelEventArgs e)
        {
            try
            {
                e.Cancel = this._cancelCheckout;
                if (e.Cancel)
                {
                    backgroundWorkerCheckOut.CancelAsync();
                }
            }
            catch (Exception)
            {

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
                    CheckOutArguments arg = e.Argument as CheckOutArguments;

                    if (arg.Type == JobType.Load)
                    {
                        e.Result = myIfsSvn.GetProjectList();
                    }
                    else if (arg.Type == JobType.CheckOut)
                    {
                        this.CheckOutProject(arg);
                        e.Result = null;
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
                ListBox myListBox = listBoxProjectList as ListBox;

                if (e.Error != null)
                {
                    Xceed.Wpf.Toolkit.MessageBox.Show(App.Current.MainWindow,
                                                      e.Error.Message,
                                                      "Error Checking out Components",
                                                      MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    if (e.Result != null)
                    {
                        List<SvnListEventArgs> nodeList = e.Result as List<SvnListEventArgs>;

                        nodeList.RemoveAt(0);

                        StackPanel treeItemStack;
                        Label lbl;
                        Image treeItemImage;
                        ListBoxItem nodeItem;
                        List<ListBoxItem> nodeItemList = new List<ListBoxItem>();
                        foreach (SvnListEventArgs project in nodeList)
                        {
                            nodeItem = new ListBoxItem();

                            treeItemStack = new StackPanel();
                            treeItemStack.Orientation = Orientation.Horizontal;

                            lbl = new Label();
                            lbl.Content = project.Name;

                            treeItemImage = new Image();
                            treeItemImage.Source = bi;

                            treeItemStack.Children.Add(treeItemImage);
                            treeItemStack.Children.Add(lbl);

                            nodeItem.Content = treeItemStack;
                            nodeItem.Tag = new SvnProject(project);
                            
                            nodeItemList.Add(nodeItem);
                        }

                        if (nodeItemList.Count > 0)
                        {
                            listBoxProjectList.ItemsSource = nodeItemList;                            
                        }

                        if (string.IsNullOrWhiteSpace(Properties.Settings.Default.SelectedProject) == false &&
                            Properties.Settings.Default.SelectedProject != "ProjectsRoot")
                        {
                            foreach (ListBoxItem item in listBoxProjectList.Items)
                            {
                                if ((item.Tag as SvnProject).Name == Properties.Settings.Default.SelectedProject)
                                {
                                    listBoxProjectList.SelectedItem = item;                     
                                    listBoxProjectList.ScrollIntoView(item);
                                    break;
                                }
                            }
                        }
                    }
                }
                buttonCheckOut.Content = "Check Out";
                textBoxLog.AppendText("Done!\r\n");

            }
            catch (Exception ex)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show(App.Current.MainWindow,
                              ex.Message,
                              "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this._cancelCheckout = false;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (backgroundWorkerCheckOut.IsBusy == false)
                {
                    backgroundWorkerCheckOut.RunWorkerAsync(new CheckOutArguments(JobType.Load));

                    textBoxWorkSpace.Text = textBoxProjectRoot.Text + @"\";
                }
            }
            catch (Exception ex)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show(App.Current.MainWindow, ex.Message, "Error Loading", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void listBoxProjectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                listBoxComponents.ItemsSource = null;
                ListBoxItem seletedNode = listBoxProjectList.SelectedItem as ListBoxItem;

                if (seletedNode != null &&
                    seletedNode.Tag != null)
                {
                    SvnProject seletedProject = seletedNode.Tag as SvnProject;
                    listBoxComponents.ItemsSource = myIfsSvn.GetComponentListFromExternals(seletedProject);
                    textBoxWorkSpace.Text = textBoxProjectRoot.Text + @"\" + seletedProject.Name + @"\" + Properties.Settings.Default.ServerWorkSpace;

                    Properties.Settings.Default.SelectedProject = seletedProject.Name;
                }
            }
            catch (Exception ex)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show(App.Current.MainWindow,
                                 ex.Message,
                                 "Error Loading Components",
                                 MessageBoxButton.OK, MessageBoxImage.Error);
            }
        } 
        
        private bool ListBoxComponentsFilter(object item)
        {
            SvnComponent component = item as SvnComponent;
            return component.Name.Contains(textBoxComponentFilter.Text);
        }

        private void textBoxComponentFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                listBoxComponents.Items.Filter = ListBoxComponentsFilter;
            }
            catch (Exception)
            {
            }
        }

        private bool ListBoxProjectsFilter(object item)
        {
            SvnProject project = (item as ListBoxItem).Tag as SvnProject;
            return project.Name.Contains(textBoxProjectsFilter.Text);
        }

        private void textBoxProjectsFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                listBoxProjectList.Items.Filter = ListBoxProjectsFilter;
            }
            catch (Exception)
            {
            }
        }

        private void textBoxProjectRoot_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                textBoxWorkSpace.Text = textBoxProjectRoot.Text + @"\";
                ListBoxItem seletedNode = listBoxProjectList.SelectedItem as ListBoxItem;
                if (seletedNode.Tag != null)
                {
                    if (seletedNode.Tag is SvnProject)
                    {
                        textBoxWorkSpace.Text += (seletedNode.Tag as SvnProject).Name + @"\" + Properties.Settings.Default.ServerWorkSpace;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void menuItemClearSelection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                listBoxComponents.UnselectAll();
            }
            catch (Exception)
            {
            }
        }

        private void menuItemSelectAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                listBoxComponents.SelectAll();
            }
            catch (Exception)
            {
            }
        }
                               
    }
}
