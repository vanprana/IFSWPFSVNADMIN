﻿using System;
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
using System.IO;
using NLog;

namespace IfsSvnClient.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlCheckOut.xaml
    /// </summary>
    public partial class UserControlCheckOut : UserControl
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private BackgroundWorker backgroundWorkerCheckOut;
        private delegate void backgroundWorkerCheckOut_RunWorkerCompletedDelegate(object sender, RunWorkerCompletedEventArgs e);
        private bool _cancelCheckout = false;
        private readonly SvnUriTarget projectsUri = new SvnUriTarget(Properties.Settings.Default.ServerUri + "/projects");
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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                textBoxWorkSpace.Text = textBoxProjectRoot.Text + @"\";
                if (backgroundWorkerCheckOut.IsBusy == false)
                {
                    progressBarMain.Visibility = System.Windows.Visibility.Visible;

                    backgroundWorkerCheckOut.RunWorkerAsync(new CheckOutArguments(JobType.Load));
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Loading Page", MessageBoxButton.OK);
            }
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
                }
                else if (this.ButtonState == ButtonState.Cancel)
                {
                    this._cancelCheckout = true;
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Checking out Components", MessageBoxButton.OK);
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

                this.Log("Starting Checking Out at " + DateTime.Now.ToString(), arg.ShowLessLogInformation);

                client.CheckOut(arg.ProjectNbprojectUri, arg.CheckOutPathNbproject, new SvnCheckOutArgs() { ThrowOnError = false, IgnoreExternals = true });

                client.CheckOut(arg.ProjectWorkspaceUri, arg.CheckOutPathWorkspace, new SvnCheckOutArgs() { IgnoreExternals = true });
                client.CheckOut(arg.ProjectDocumentUri, arg.CheckOutPathDocument, new SvnCheckOutArgs() { IgnoreExternals = true });

                this.Log("Starting Component Checking Out at " + DateTime.Now.ToString(), arg.ShowLessLogInformation);

                Uri componentUri;
                foreach (SvnComponent component in arg.CompornentArray)
                {
                    componentUri = new Uri(component.Path.Replace("^/", rootUri.AbsoluteUri));

                    this.Log("Component: " + component.Name, arg.ShowLessLogInformation);

                    if (component.Type == SvnComponent.SvnComponentType.Work)
                    {
                        client.CheckOut(componentUri, arg.CheckOutPathWorkspace + @"\" + component.Name);
                    }
                    else if(component.Type == SvnComponent.SvnComponentType.Document)
                    {
                        client.CheckOut(componentUri, arg.CheckOutPathDocument + @"\" + component.Name);
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
                    ModernDialog.ShowMessage(e.Error.Message, "Error Checking out Components", MessageBoxButton.OK);
                }
                else
                {
                    if (e.Result != null)
                    {
                        List<SvnListEventArgs> nodeList = e.Result as List<SvnListEventArgs>;

                        nodeList.RemoveAt(0);

                        StackPanel treeItemStack;
                        TextBlock lbl;
                        Image treeItemImage;
                        ListBoxItem nodeItem;
                        List<ListBoxItem> nodeItemList = new List<ListBoxItem>();
                        foreach (SvnListEventArgs project in nodeList)
                        {
                            nodeItem = new ListBoxItem();

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
                this.ButtonState = ButtonState.CheckOut;
                this.Log("Done!", checkBoxShowLessInfor.IsChecked.Value);
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error", MessageBoxButton.OK);
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

                    textBoxWorkSpace.Text = textBoxProjectRoot.Text + @"\" + seletedProject.Name + @"\" + Properties.Settings.Default.ServerWorkSpace;

                    Properties.Settings.Default.SelectedProject = seletedProject.Name;

                    listBoxComponents.SelectedItem = null;
                    listBoxComponents.SelectedItems.Clear();
                    listBoxComponents.UnselectAll();
                }
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error Loading Components", MessageBoxButton.OK);
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
                        textBoxWorkSpace.Text += (seletedNode.Tag as SvnProject).Name + @"\" + Properties.Settings.Default.ServerWorkSpace;
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
                System.Diagnostics.Process.Start("explorer", textBoxWorkSpace.Text);
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error navigation to folder path", MessageBoxButton.OK);
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
            }
            catch (Exception ex)
            {
                ModernDialog.ShowMessage(ex.Message, "Error selecting Project-Root folder", MessageBoxButton.OK);
            }
        }

        private void buttonComponentsSelectAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                listBoxComponents.SelectAll();
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
                }
            }
            catch (Exception)
            {
            }
        }
    }
}