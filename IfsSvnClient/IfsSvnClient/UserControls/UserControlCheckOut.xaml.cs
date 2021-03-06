﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Controls;
using IfsSvnClient.Classes;
using NLog;
using SharpSvn;
using SharpSvn.UI;

namespace IfsSvnClient.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlCheckOut.xaml
    /// </summary>
    public partial class UserControlCheckOut : UserControl, IContent
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private BackgroundWorker backgroundWorkerCheckOut;

        private delegate void backgroundWorkerCheckOut_RunWorkerCompletedDelegate(object sender, RunWorkerCompletedEventArgs e);

        private bool _cancelCheckout = false;
        private SvnUriTarget projectsUri;

        private delegate void client_NotifyDelegate(object sender, SvnNotifyEventArgs e);

        private delegate void logDelegate(string message, bool showLessLogInformation);

        private BitmapImage projectImage;
        private BitmapImage componentImage;
        private BitmapImage checkOutImage;
        private BitmapImage cancelImage;
        private IfsSvn myIfsSvn;

        public UserControlCheckOut()
        {
            InitializeComponent();

            myIfsSvn = new IfsSvn();

            this.backgroundWorkerCheckOut = new BackgroundWorker();
            this.backgroundWorkerCheckOut.WorkerSupportsCancellation = true;
            this.backgroundWorkerCheckOut.DoWork += new DoWorkEventHandler(this.backgroundWorkerCheckOut_DoWork);
            this.backgroundWorkerCheckOut.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.backgroundWorkerCheckOut_RunWorkerCompleted);

            projectImage = new BitmapImage();
            projectImage.BeginInit();
            projectImage.UriSource = new Uri(@"/IfsSvnClient;component/Resources/project.png", UriKind.RelativeOrAbsolute);
            projectImage.EndInit();

            componentImage = new BitmapImage();
            componentImage.BeginInit();
            componentImage.UriSource = new Uri(@"/IfsSvnClient;component/Resources/folder.png", UriKind.RelativeOrAbsolute);
            componentImage.EndInit();

            checkOutImage = new BitmapImage();
            checkOutImage.BeginInit();
            checkOutImage.UriSource = new Uri(@"/IfsSvnClient;component/Resources/checkout.png", UriKind.RelativeOrAbsolute);
            checkOutImage.EndInit();

            cancelImage = new BitmapImage();
            cancelImage.BeginInit();
            cancelImage.UriSource = new Uri(@"/IfsSvnClient;component/Resources/cancel.png", UriKind.RelativeOrAbsolute);
            cancelImage.EndInit();
        }

        private ButtonState ButtonState
        {
            get
            {
                string buttonText = ((buttonCheckOut.Content as StackPanel).Children[1] as Label).Content.ToString();
                if (buttonText == "Check Out")
                {
                    return ButtonState.CheckOut;
                }
                else
                {
                    return ButtonState.Cancel;
                }
            }
            set
            {
                ImageSource source = null;
                string buttonText = string.Empty;
                if (value == Classes.ButtonState.CheckOut)
                {
                    buttonText = "Check Out";
                    source = this.checkOutImage;
                }
                else
                {
                    buttonText = "Cancel";
                    source = this.cancelImage;
                }
                ((buttonCheckOut.Content as StackPanel).Children[0] as Image).Source = source;
                ((buttonCheckOut.Content as StackPanel).Children[1] as Label).Content = buttonText;
            }
        }

        private void buttonCheckOut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.ButtonState == ButtonState.CheckOut)
                {
                    this.StartCheckOut();

                    logger.Info("buttonCheckOut: checkout, Show Less Log information {1}", checkBoxShowLessInfor.IsChecked);
                }
                else if (this.ButtonState == ButtonState.Cancel)
                {
                    this._cancelCheckout = true;

                    logger.Info("buttonCheckOut: Cancel");
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Checking out Components", MessageBoxButton.OK);
                logger.Error("Error Checking out Components", ex);
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
                        progressBarMain.Visibility = System.Windows.Visibility.Visible;
                        this.ButtonState = ButtonState.Cancel;

                        string projectPath = ((listBoxProjectList.SelectedItem as ListBoxItem).Tag as SvnProject).Path;

                        ListBoxItem[] listItemArray = new ListBoxItem[listBoxComponents.SelectedItems.Count];
                        listBoxComponents.SelectedItems.CopyTo(listItemArray, 0);

                        SvnComponent[] compornentArray = listItemArray.Select(i => i.Tag as SvnComponent).ToArray();

                        string checkOutPathProject = textBoxProjectRoot.Text + @"\";
                        ListBoxItem seletedNode = listBoxProjectList.SelectedItem as ListBoxItem;
                        if (seletedNode.Tag != null)
                        {
                            if (seletedNode.Tag is SvnProject)
                            {
                                checkOutPathProject += (seletedNode.Tag as SvnProject).Name + @"\";
                            }
                        }
                        Mouse.OverrideCursor = Cursors.Wait;
                        backgroundWorkerCheckOut.RunWorkerAsync(new CheckOutArguments(JobType.CheckOut,
                                                                                      checkBoxShowLessInfor.IsChecked.Value,
                                                                                      projectPath,
                                                                                      checkOutPathProject,
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

                if (arg.ShowLessLogInformation == false)
                {
                    client.Notify += new EventHandler<SvnNotifyEventArgs>(client_Notify);
                }
                client.Cancel += new EventHandler<SvnCancelEventArgs>(client_Cancel);

                Uri rootUri = client.GetRepositoryRoot(arg.ProjectWorkspaceUri);

                this.Log("Starting Checking Out at " + DateTime.Now.ToString(), false);

                client.CheckOut(arg.ProjectNbprojectUri, arg.CheckOutPathNbproject, new SvnCheckOutArgs() { ThrowOnError = false, IgnoreExternals = true });

                client.CheckOut(arg.ProjectWorkspaceUri, arg.CheckOutPathWorkspace, new SvnCheckOutArgs() { IgnoreExternals = true });
                client.CheckOut(arg.ProjectDocumentUri, arg.CheckOutPathDocument, new SvnCheckOutArgs() { IgnoreExternals = true });

                this.Log("Starting Component Checking Out at " + DateTime.Now.ToString(), arg.ShowLessLogInformation);

                Uri componentUri;
                if (arg.HasDocCompornents)
                {
                    componentUri = new Uri(Properties.Resources.ServerDocumentationOnlinedocframework.Replace("^/", rootUri.AbsoluteUri));

                    client.CheckOut(componentUri, arg.CheckOutPathDocumentEn);
                }

                foreach (SvnComponent component in arg.CompornentArray)
                {
                    componentUri = new Uri(component.Path.Replace("^/", rootUri.AbsoluteUri));

                    this.Log("Component: " + component.Name, arg.ShowLessLogInformation);

                    if (component.Type == SvnComponent.SvnComponentType.Work)
                    {
                        client.CheckOut(componentUri, arg.CheckOutPathWorkspace + @"\" + component.Name);
                    }
                    else if (component.Type == SvnComponent.SvnComponentType.Document)
                    {
                        List<SvnListEventArgs> folderList = myIfsSvn.GetFolderList(componentUri);
                        folderList.RemoveAt(0);
                        foreach (SvnListEventArgs folder in folderList)
                        {
                            client.CheckOut(folder.Uri, arg.CheckOutPathDocumentEn + @"\" + folder.Name);
                        }
                    }
                }
            }
        }

        private void client_Notify(object sender, SvnNotifyEventArgs e)
        {
            try
            {
                if (MyScrollViewer.Dispatcher.CheckAccess())
                {
                    if (e.Error != null)
                    {
                        textBlockLog.Inlines.Add(new Run(e.Error.Message + "\r\n"));
                    }
                    else
                    {
                        textBlockLog.Inlines.Add(new Italic(new Run(e.Action.ToString())) { Foreground = Brushes.Gray });
                        if (e.Action == SvnNotifyAction.UpdateCompleted)
                        {
                            textBlockLog.Inlines.Add(new Run(" " + e.Path));
                        }
                        else
                        {
                            textBlockLog.Inlines.Add(new Run(" \t" + e.Path));
                        }
                        textBlockLog.Inlines.Add(new LineBreak());
                    }
                    MyScrollViewer.ScrollToEnd();
                }
                else
                {
                    MyScrollViewer.Dispatcher.Invoke(new client_NotifyDelegate(client_Notify), new object[] { sender, e });
                }
            }
            catch (Exception)
            {
            }
        }

        private void Log(string message, bool showLessLogInformation)
        {
            try
            {
                if (MyScrollViewer.Dispatcher.CheckAccess())
                {
                    if (showLessLogInformation)
                    {
                        textBlockLog.Inlines.Add(new Run(message + "\r\n"));
                    }
                    else
                    {
                        textBlockLog.Inlines.Add(new Bold(new Run(message + "\r\n")));
                    }
                    MyScrollViewer.ScrollToEnd();
                }
                else
                {
                    MyScrollViewer.Dispatcher.Invoke(new logDelegate(Log), new object[] { message, showLessLogInformation });
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

        private void LoadTree(List<SvnListEventArgs> nodeList)
        {
            if (nodeList != null && nodeList.Count > 0)
            {
                nodeList.RemoveAt(0);

                StackPanel treeItemStack;
                TextBlock lbl;
                Image treeItemImage;
                ListBoxItem nodeItem;
                List<ListBoxItem> nodeItemList = new List<ListBoxItem>();
                foreach (SvnListEventArgs project in nodeList)
                {
                    nodeItem = new ListBoxItem();
                    nodeItem.Name = project.Name.Replace("-", "_").Replace(".", "_");

                    treeItemStack = new StackPanel();
                    treeItemStack.Orientation = Orientation.Horizontal;

                    lbl = new TextBlock();
                    lbl.Text = project.Name;
                    lbl.Margin = new Thickness(3, 1, 3, 1);

                    treeItemImage = new Image();
                    treeItemImage.Source = projectImage;
                    treeItemImage.Margin = new Thickness(3, 1, 3, 1);

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

                if (Properties.Settings.Default.RememberFiltering)
                {
                    textBoxProjectsFilter.Text = Properties.Settings.Default.TextBoxProjectsFilter_text;
                    textBoxComponentFilter.Text = Properties.Settings.Default.TextBoxComponentFilter_text;
                }

                if (Properties.Settings.Default.SelectCheckedOutAtStartUp)
                {
                    this.SelectCheckedOutComponents();
                }
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
                        e.Result = JobType.CheckOut;
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
                    ModernDialog.ShowMessage(e.Error.Message, "Error Checking out Components", MessageBoxButton.OK);
                    logger.Error("Error Checking out Components", e.Error);
                }
                else
                {
                    if (e.Result != null)
                    {
                        if (e.Result is List<SvnListEventArgs>)
                        {
                            this.LoadTree(e.Result as List<SvnListEventArgs>);
                            this.Log("Every thing is loaded now.", checkBoxShowLessInfor.IsChecked.Value);
                        }
                        else
                        {
                            this.Log("Finished Checking Out.", false);
                        }
                    }
                }
                this.ButtonState = ButtonState.CheckOut;
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
                    List<SvnComponent> componentList = myIfsSvn.GetComponentListFromExternals(seletedProject);
                    componentList.AddRange(myIfsSvn.GetDocumentComponentListFromExternals(seletedProject));

                    StackPanel treeItemStack;
                    TextBlock lbl;
                    Image treeItemImage;
                    ListBoxItem nodeItem;
                    List<ListBoxItem> nodeItemList = new List<ListBoxItem>();

                    foreach (SvnComponent component in componentList)
                    {
                        nodeItem = new ListBoxItem();
                        nodeItem.Name = component.Name.Replace("-", "_").Replace(".", "_");

                        treeItemStack = new StackPanel();
                        treeItemStack.Orientation = Orientation.Horizontal;

                        lbl = new TextBlock();
                        lbl.Text = component.Name;
                        lbl.Margin = new Thickness(3, 1, 3, 1);

                        treeItemImage = new Image();
                        treeItemImage.Source = componentImage;
                        treeItemImage.Margin = new Thickness(3, 1, 3, 1);

                        treeItemStack.Children.Add(treeItemImage);
                        treeItemStack.Children.Add(lbl);

                        nodeItem.Content = treeItemStack;
                        nodeItem.Tag = component;

                        nodeItemList.Add(nodeItem);
                    }

                    if (nodeItemList.Count > 0)
                    {
                        listBoxComponents.ItemsSource = nodeItemList;
                    }

                    textBoxWorkSpace.Text = textBoxProjectRoot.Text + @"\" + seletedProject.Name + @"\" + Properties.Resources.CheckOutPath_WorkSpace;

                    Properties.Settings.Default.SelectedProject = seletedProject.Name;

                    listBoxComponents.SelectedItem = null;
                    listBoxComponents.SelectedItems.Clear();
                    listBoxComponents.UnselectAll();
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Loading Components", MessageBoxButton.OK);
                logger.Error("Error Loading Components", ex);
            }
        }

        private bool ListBoxComponentsFilter(object item)
        {
            SvnComponent component = (item as ListBoxItem).Tag as SvnComponent;
            return component.Name.Contains(textBoxComponentFilter.Text);
        }

        private void textBoxComponentFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                listBoxComponents.Items.Filter = ListBoxComponentsFilter;

                if (Properties.Settings.Default.TextBoxComponentFilter_text != textBoxComponentFilter.Text)
                {
                    logger.Info("textBoxComponentFilter: {0}", textBoxComponentFilter.Text);
                }

                if (Properties.Settings.Default.RememberFiltering)
                {
                    Properties.Settings.Default.TextBoxComponentFilter_text = textBoxComponentFilter.Text;
                }

                listBoxComponents.SelectedItem = null;
                listBoxComponents.SelectedItems.Clear();
                listBoxComponents.UnselectAll();
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

                if (Properties.Settings.Default.TextBoxProjectsFilter_text != textBoxProjectsFilter.Text)
                {
                    logger.Info("textBoxProjectsFilter: {0}", textBoxProjectsFilter.Text);
                }

                if (Properties.Settings.Default.RememberFiltering)
                {
                    Properties.Settings.Default.TextBoxProjectsFilter_text = textBoxProjectsFilter.Text;
                }
            }
            catch (Exception)
            {
            }
        }

        private void textBoxProjectRoot_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string projectRoot = textBoxProjectRoot.Text;
                if (projectRoot.EndsWith(@"\") == false)
                {
                    projectRoot += @"\";
                }
                if (Directory.Exists(projectRoot))
                {
                    textBoxProjectRoot.BorderBrush = (new BrushConverter().ConvertFrom("#FFCCCCCC") as SolidColorBrush);
                }
                else
                {
                    textBoxProjectRoot.BorderBrush = Brushes.Red;
                }

                textBoxWorkSpace.Text = projectRoot;
                ListBoxItem seletedNode = listBoxProjectList.SelectedItem as ListBoxItem;
                if (seletedNode.Tag != null)
                {
                    if (seletedNode.Tag is SvnProject)
                    {
                        textBoxWorkSpace.Text += (seletedNode.Tag as SvnProject).Name + @"\" + Properties.Resources.CheckOutPath_WorkSpace;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private bool SelectCheckedOutComponents()
        {
            listBoxComponents.SelectedItem = null;
            listBoxComponents.SelectedItems.Clear();
            if (this.ValidateWorkSpacePath())
            {
                string workSpace = textBoxWorkSpace.Text;
                if (workSpace.EndsWith(@"\") == false)
                {
                    workSpace += @"\";
                }

                SvnComponent component;
                foreach (ListBoxItem item in listBoxComponents.Items)
                {
                    component = item.Tag as SvnComponent;
                    item.IsSelected = Directory.Exists(workSpace + component.Name);
                    if (item.IsSelected)
                    {
                        listBoxComponents.SelectedItems.Add(item);
                    }
                }
            }

            return (listBoxComponents.SelectedItems.Count > 0);
        }

        private void buttonGoToPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                logger.Info("buttonGoToPath: {0}", textBoxWorkSpace.Text);

                System.Diagnostics.Process.Start("explorer", textBoxWorkSpace.Text);
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error navigation to folder path", MessageBoxButton.OK);
                logger.Error("Error navigation to folder path", ex);
            }
        }

        private void textBoxWorkSpace_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                ValidateWorkSpacePath();
            }
            catch (Exception)
            {
            }
        }

        private bool ValidateWorkSpacePath()
        {
            buttonGoToPath.IsEnabled = Directory.Exists(textBoxWorkSpace.Text);
            if (buttonGoToPath.IsEnabled)
            {
                buttonGoToPath.Opacity = 1;
                textBoxWorkSpace.BorderBrush = (new BrushConverter().ConvertFrom("#FFCCCCCC") as SolidColorBrush);
            }
            else
            {
                buttonGoToPath.Opacity = 0.5;
                textBoxWorkSpace.BorderBrush = Brushes.Red;
            }
            return buttonGoToPath.IsEnabled;
        }

        private void buttonProjectRoot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "Please Select your Project-Root folder";

                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    textBoxProjectRoot.Text = dialog.SelectedPath;
                    Properties.Settings.Default.ProjectRoot = textBoxProjectRoot.Text;
                }

                logger.Info("buttonProjectRoot: {0}", textBoxProjectRoot.Text);
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error selecting Project-Root folder", MessageBoxButton.OK);
                logger.Error("Error selecting Project-Root folder", ex);
            }
        }

        private void buttonComponentsSelectAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                listBoxComponents.SelectAll();

                logger.Info("buttonComponentsSelectAll");
            }
            catch (Exception)
            {
            }
        }

        private void buttonComponentsUnselectAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                listBoxComponents.UnselectAll();

                logger.Info("buttonComponentsUnselectAll");
            }
            catch (Exception)
            {
            }
        }

        private void buttonComponentsSelectCheckedOut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.SelectCheckedOutComponents();

                logger.Info("buttonComponentsSelectCheckedOut");
            }
            catch (Exception)
            {
            }
        }

        private void buttonClearLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //textBoxLog.Clear();
                textBlockLog.Inlines.Clear();

                logger.Info("buttonClearLog");
            }
            catch (Exception)
            {
            }
        }

        private void buttonCopyLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textBlockLog.Text) == false)
                {
                    Clipboard.SetText(textBlockLog.Text);

                    ModernDialog.ShowMessage("Log copied to clipboard", "Log Copied", MessageBoxButton.OK);

                    logger.Info("buttonCopyLog");
                }
            }
            catch (Exception)
            {
            }
        }

        #region Navigation

        public void OnFragmentNavigation(FirstFloor.ModernUI.Windows.Navigation.FragmentNavigationEventArgs e)
        {
        }

        public void OnNavigatedFrom(FirstFloor.ModernUI.Windows.Navigation.NavigationEventArgs e)
        {
        }

        public void OnNavigatedTo(FirstFloor.ModernUI.Windows.Navigation.NavigationEventArgs e)
        {
            try
            {
                if (e.NavigationType == FirstFloor.ModernUI.Windows.Navigation.NavigationType.New)
                {
                    if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ServerUri) == false)
                    {
                        projectsUri = new SvnUriTarget(Properties.Settings.Default.ServerUri + "/projects");

                        textBoxWorkSpace.Text = textBoxProjectRoot.Text + @"\";
                        if (backgroundWorkerCheckOut.IsBusy == false)
                        {
                            progressBarMain.Visibility = System.Windows.Visibility.Visible;

                            backgroundWorkerCheckOut.RunWorkerAsync(new CheckOutArguments(JobType.Load));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Loading Page", MessageBoxButton.OK);
                logger.Error("Error Loading Page", ex);
            }
        }

        public void OnNavigatingFrom(FirstFloor.ModernUI.Windows.Navigation.NavigatingCancelEventArgs e)
        {
        }

        #endregion
    }
}