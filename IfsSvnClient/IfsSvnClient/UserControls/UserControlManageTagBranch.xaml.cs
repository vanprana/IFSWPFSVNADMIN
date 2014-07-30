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
using IfsSvnClient.Classes;
using SharpSvn;
using SharpSvn.UI;
using System.Collections.ObjectModel;
using FirstFloor.ModernUI.Windows.Controls;

namespace IfsSvnClient.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlManageTagBranch.xaml
    /// </summary>
    public partial class UserControlManageTagBranch : UserControl
    {
        private BackgroundWorker backgroundWorkerLoad;
        private delegate void backgroundWorkerLoad_RunWorkerCompletedDelegate(object sender, RunWorkerCompletedEventArgs e);

        private BitmapImage rootBi;
        private BitmapImage bi;

        private IfsSvn myIfsSvn;

        public UserControlManageTagBranch()
        {
            InitializeComponent();

            myIfsSvn = new IfsSvn();

            this.backgroundWorkerLoad = new BackgroundWorker();
            this.backgroundWorkerLoad.WorkerSupportsCancellation = true;
            this.backgroundWorkerLoad.DoWork += new DoWorkEventHandler(this.backgroundWorkerLoad_DoWork);
            this.backgroundWorkerLoad.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.backgroundWorkerLoad_RunWorkerCompleted);

            rootBi = new BitmapImage();
            rootBi.BeginInit();
            rootBi.UriSource = new Uri(@"/IfsSvnClient;component/Resources/project.png", UriKind.RelativeOrAbsolute);
            rootBi.EndInit();

            bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(@"/IfsSvnClient;component/Resources/folder.png", UriKind.RelativeOrAbsolute);
            bi.EndInit();
        }

        private void backgroundWorkerLoad_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Do not access the form's BackgroundWorker reference directly.
                // Instead, use the reference provided by the sender parameter.
                BackgroundWorker worker = sender as BackgroundWorker;

                if (worker.CancellationPending == false)
                {
                    TagArguments arg = e.Argument as TagArguments;

                    if (arg.Type == JobType.LoadComponents)
                    {
                        e.Result = myIfsSvn.GetComponentList();
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

        private void backgroundWorkerLoad_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this.Dispatcher.CheckAccess() == false)
            {
                this.Dispatcher.Invoke(new backgroundWorkerLoad_RunWorkerCompletedDelegate(backgroundWorkerLoad_RunWorkerCompleted), new object[] { sender, e });
            }
            else
            {
                try
                {
                    if (e.Error != null)
                    {
                        ModernDialog.ShowMessage(e.Error.Message, "Error setting Log", MessageBoxButton.OK);
                    }
                    else if (e.Cancelled)
                    {
                        //    textBoxLog.AppendText("Cancelled!\r\n");
                    }
                    else
                    {
                        if (e.Result != null)
                        {
                            List<SvnListEventArgs> forlderList = e.Result as List<SvnListEventArgs>;


                            List<SvnComponent> componentList = new List<SvnComponent>();
                            foreach (SvnListEventArgs folder in forlderList)
                            {
                                if (string.IsNullOrWhiteSpace(folder.Path) == false)
                                {
                                    componentList.Add(new SvnComponent(folder));
                                }
                            }
                            listBoxComponentList.ItemsSource = componentList;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModernDialog.ShowMessage(ex.Message, "Error", MessageBoxButton.OK);
                }
                finally
                {
                    //buttonFind.Content = "Find";
                    progressBarMain.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (backgroundWorkerLoad.IsBusy == false)
                {
                    progressBarMain.Visibility = System.Windows.Visibility.Visible;

                    backgroundWorkerLoad.RunWorkerAsync(new TagArguments(JobType.LoadComponents));
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Loading", MessageBoxButton.OK);
            }
        }

        private void listBoxComponentList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                treeViewComponent.Items.Clear();
                SvnComponent selectedComponent = listBoxComponentList.SelectedItem as SvnComponent;

                if (selectedComponent != null)
                {
                    List<SvnListEventArgs> forlderList = myIfsSvn.GetFolderList(new SvnUriTarget(selectedComponent.Path));

                    StackPanel treeItemStack = new StackPanel();
                    treeItemStack.Orientation = Orientation.Horizontal;

                    Label lbl = new Label();
                    lbl.Content = forlderList[0].Name;

                    Image treeItemImage = new Image();
                    treeItemImage.Source = rootBi;

                    treeItemStack.Children.Add(treeItemImage);
                    treeItemStack.Children.Add(lbl);

                    TreeViewItem rootNodeItem = new TreeViewItem();
                    rootNodeItem.Header = treeItemStack;
                    rootNodeItem.Tag = forlderList[0];
                    rootNodeItem.IsExpanded = true;

                    forlderList.RemoveAt(0);

                    TreeViewItem nodeItem;
                    List<TreeViewItem> nodeItemList = new List<TreeViewItem>();
                    foreach (SvnListEventArgs folder in forlderList)
                    {
                        nodeItem = new TreeViewItem();

                        treeItemStack = new StackPanel();
                        treeItemStack.Orientation = Orientation.Horizontal;

                        lbl = new Label();
                        lbl.Content = folder.Name;

                        treeItemImage = new Image();
                        treeItemImage.Source = bi;

                        treeItemStack.Children.Add(treeItemImage);
                        treeItemStack.Children.Add(lbl);

                        nodeItem.Header = treeItemStack;
                        nodeItem.Tag = folder;

                        nodeItem.Expanded += new RoutedEventHandler(nodeItem_Expanded);

                        nodeItemList.Add(nodeItem);
                    }

                    if (nodeItemList.Count > 0)
                    {
                        rootNodeItem.ItemsSource = nodeItemList;
                        treeViewComponent.Items.Add(rootNodeItem);
                    }
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Loading", MessageBoxButton.OK);
            }
        }

        private void nodeItem_Expanded(object sender, RoutedEventArgs e)
        {
            try
            {
                TreeViewItem selectedFolderView = e.OriginalSource as TreeViewItem;
                if (selectedFolderView != null)
                {
                    SvnListEventArgs selectedFolder = selectedFolderView.Tag as SvnListEventArgs;

                    List<SvnListEventArgs> childForlderList = myIfsSvn.GetFolderList(selectedFolder.Uri);

                    childForlderList.RemoveAt(0);

                    StackPanel treeItemStack;
                    TextBlock lbl;
                    Image treeItemImage;

                    TreeViewItem nodeItem;
                    List<TreeViewItem> nodeItemList = new List<TreeViewItem>();
                    foreach (SvnListEventArgs childForlder in childForlderList)
                    {
                        nodeItem = new TreeViewItem();

                        treeItemStack = new StackPanel();
                        treeItemStack.Orientation = Orientation.Horizontal;

                        treeItemImage = new Image();
                        treeItemImage.Source = bi;
                        treeItemStack.Children.Add(treeItemImage);

                        lbl = new TextBlock();
                        lbl.Text = childForlder.Name;
                        treeItemStack.Children.Add(lbl);

                        nodeItem.Header = treeItemStack;
                        nodeItem.Tag = childForlder;

                        nodeItem.Expanded += new RoutedEventHandler(nodeItem_Expanded);

                        nodeItemList.Add(nodeItem);
                    }

                    selectedFolderView.ItemsSource = nodeItemList;
                }
            }
            catch (Exception)
            {

            }
        }

        private TreeViewItem GetSelectedTreeViewItemParent(TreeViewItem item)
        {
            try
            {
                DependencyObject parent = VisualTreeHelper.GetParent(item);
                while ((parent is TreeViewItem) == false)
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }
                return parent as TreeViewItem;
            }
            catch (Exception)
            {

            }
            return null;
        }

        private void treeViewComponent_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                TreeViewItem selectedFolderView = treeViewComponent.SelectedItem as TreeViewItem;
                if (selectedFolderView != null)
                {
                    TreeViewItem selectedParentFolderView = this.GetSelectedTreeViewItemParent(selectedFolderView);

                    if (selectedParentFolderView != null)
                    {
                        string selectedFolder_parentFolder_name = ((selectedParentFolderView.Header as StackPanel).Children[1] as Label).Content.ToString();

                        if (selectedFolder_parentFolder_name != "tags")
                        {
                            userControlCreateBranchFromSelectedTag.Visibility = System.Windows.Visibility.Collapsed;

                            gridMain.ColumnDefinitions[Grid.GetColumn(treeViewComponent)].Width = new GridLength(100, GridUnitType.Star);
                        }
                        else
                        {
                            userControlCreateBranchFromSelectedTag.SetSelectedTag(selectedFolderView.Tag as SvnListEventArgs);

                            userControlCreateBranchFromSelectedTag.Visibility = System.Windows.Visibility.Visible;
                            gridMain.ColumnDefinitions[Grid.GetColumn(treeViewComponent)].Width = GridLength.Auto;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }

    }
}
