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

        public UserControlCheckOut()
        {
            InitializeComponent();

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
                if (buttonCheckOut.Content.ToString() == "CheckOut")
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
                    if (treeViewProjectList.SelectedItem != null &&
                        (treeViewProjectList.SelectedItem as TreeViewItem).Tag != null)
                    {
                        buttonCheckOut.Content = "Cancel";

                        string projectPath = ((treeViewProjectList.SelectedItem as TreeViewItem).Tag as SvnProject).Path;

                        SvnComponent[] compornentArray = new SvnComponent[listBoxComponents.SelectedItems.Count];
                        listBoxComponents.SelectedItems.CopyTo(compornentArray, 0);
                        Uri projectUri = new Uri(projectPath + Properties.Settings.Default.ServerWorkSpace + "/");

                        backgroundWorkerCheckOut.RunWorkerAsync(new CheckOutArguments(CheckOutType.CheckOut,
                                                                                      projectUri, 
                                                                                      textBoxCheckOutPath.Text, 
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

                    if (arg.Type == CheckOutType.Load)
                    {
                        e.Result = this.LoadProjects();
                    }
                    else if (arg.Type == CheckOutType.CheckOut)
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

        private List<SvnProject> LoadProjects()
        {
            try
            {
                using (SvnClient client = new SvnClient())
                {
                    // Bind the SharpSvn UI to our client for SSL certificate and credentials
                    SvnUIBindArgs bindArgs = new SvnUIBindArgs();
                    SvnUI.Bind(client, bindArgs);
                                        
                    Collection<SvnListEventArgs> projectList;                    
                    List<SvnProject> nodeList = new List<SvnProject>();
                    if (client.GetList(this.projectsUri, out projectList))
                    {
                        foreach (SvnListEventArgs project in projectList)
                        {
                            if (project.Entry.NodeKind == SvnNodeKind.Directory &&
                                string.IsNullOrWhiteSpace(project.Path) == false)
                            {                               
                                nodeList.Add(new SvnProject(project));
                            }
                        }
                    }

                    return nodeList;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void backgroundWorkerCheckOut_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                TreeView myTreeView = treeViewProjectList as TreeView;

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
                        List<SvnProject> nodeList = e.Result as List<SvnProject>;
                        TreeViewItem rootNode = myTreeView.Items[0] as TreeViewItem;
                        rootNode.IsExpanded = true;

                        StackPanel treeItemStack;
                        Label lbl;
                        Image treeItemImage;
                        TreeViewItem nodeItem;
                        List<TreeViewItem> nodeItemList = new List<TreeViewItem>(); 
                        foreach (SvnProject project in nodeList)
                        {
                            nodeItem = new TreeViewItem();

                            treeItemStack = new StackPanel();
                            treeItemStack.Orientation = Orientation.Horizontal;

                            lbl = new Label();
                            lbl.Content = project.Name;

                            treeItemImage = new Image();
                            treeItemImage.Source = bi;

                            treeItemStack.Children.Add(treeItemImage);
                            treeItemStack.Children.Add(lbl);

                            nodeItem.Header = treeItemStack;
                            nodeItem.Tag = project;
                            
                            nodeItemList.Add(nodeItem);
                        }

                        if (nodeItemList.Count > 0)
                        {
                            rootNode.ItemsSource = nodeItemList;                            
                        }

                        if (string.IsNullOrWhiteSpace(Properties.Settings.Default.SelectedProject) == false &&
                            Properties.Settings.Default.SelectedProject != "ProjectsRoot")
                        {
                            foreach (TreeViewItem item in rootNode.Items)
                            {
                                if ((item.Tag as SvnProject).Name == Properties.Settings.Default.SelectedProject)
                                {
                                    item.IsSelected = true;
                                    item.BringIntoView();
                                    break;
                                }
                            }
                        }
                    }
                }
                buttonCheckOut.Content = "CheckOut";
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
                    backgroundWorkerCheckOut.RunWorkerAsync(new CheckOutArguments(CheckOutType.Load));

                    textBoxCheckOutPath.Text = textBoxWorkSpace.Text + @"\";
                }
            }
            catch (Exception ex)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show(App.Current.MainWindow, ex.Message, "Error Loading", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void treeViewProjectList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                listBoxComponents.Items.Clear();
                TreeViewItem seletedNode = e.NewValue as TreeViewItem;

                if (seletedNode.Tag != null)
                {
                    SvnProject seletedProject = seletedNode.Tag as SvnProject;

                    using (SvnClient client = new SvnClient())
                    {
                        SvnUriTarget projectUri = new SvnUriTarget(seletedProject.Path + Properties.Settings.Default.ServerWorkSpace + "/");

                        string components;
                        client.TryGetProperty(projectUri, SvnPropertyNames.SvnExternals, out components);

                        if (string.IsNullOrWhiteSpace(components) == false)
                        {
                            string[] componentInforArray = components.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                            SvnComponent component;
                            foreach (string componentInfor in componentInforArray)
                            {
                                component = new SvnComponent(componentInfor);
                                listBoxComponents.Items.Add(component);
                            }
                        }
                    }
                    textBoxCheckOutPath.Text = textBoxWorkSpace.Text + @"\" + seletedProject.Name + @"\" + Properties.Settings.Default.ServerWorkSpace;

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

        private void textBoxWorkSpace_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                textBoxCheckOutPath.Text = textBoxWorkSpace.Text + @"\";
                if (treeViewProjectList.SelectedItem is SvnProject)
                {
                    textBoxCheckOutPath.Text += (treeViewProjectList.SelectedItem as SvnProject).Name + @"\" + Properties.Settings.Default.ServerWorkSpace;
                }
            }
            catch (Exception)
            {
            }
        }

        private void treeViewProjectList_KeyDown(object sender, KeyEventArgs e)
        {
            
        }
    }
}
