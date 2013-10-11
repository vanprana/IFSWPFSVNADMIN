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
using SharpSvn;
using IfsSvnAdmin.Classes;
using System.ComponentModel;

namespace IfsSvnAdmin.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlCreateBranchFromTag.xaml
    /// </summary>
    public partial class UserControlCreateBranchFromTag : UserControl
    {
        private BackgroundWorker backgroundWorkerLoad;
        private delegate void backgroundWorkerLoad_RunWorkerCompletedDelegate(object sender, RunWorkerCompletedEventArgs e);
        
        private SvnListEventArgs selectedTag;

        private IfsSvn myIfsSvn;

        public UserControlCreateBranchFromTag()
        {
            InitializeComponent();

            myIfsSvn = new IfsSvn();

            this.backgroundWorkerLoad = new BackgroundWorker();
            this.backgroundWorkerLoad.WorkerSupportsCancellation = true;
            this.backgroundWorkerLoad.DoWork += new DoWorkEventHandler(this.backgroundWorkerLoad_DoWork);
            this.backgroundWorkerLoad.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.backgroundWorkerLoad_RunWorkerCompleted);
        }

        internal void SetSelectedTag(SvnListEventArgs tag)
        {
            this.selectedTag = tag;
            textBoxSelectedTag.Text = this.selectedTag.Name;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (backgroundWorkerLoad.IsBusy == false)
                {
                    backgroundWorkerLoad.RunWorkerAsync(new TagArguments(JobType.LoadProjects));
                }
            }
            catch (Exception ex)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show(App.Current.MainWindow, ex.Message, "Error Loading", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                    if (arg.Type == JobType.LoadProjects)
                    {
                        e.Result = myIfsSvn.GetProjectList();
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
                        Xceed.Wpf.Toolkit.MessageBox.Show(App.Current.MainWindow,
                                                      e.Error.Message,
                                                      "Error setting Log",
                                                      MessageBoxButton.OK, MessageBoxImage.Error);
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

                            List<SvnProject> projectList = new List<SvnProject>();
                            foreach (SvnListEventArgs folder in forlderList)
                            {
                                if (string.IsNullOrWhiteSpace(folder.Path) == false)
                                {
                                    projectList.Add(new SvnProject(folder));
                                }
                            }
                            SvnProject selectedProject = projectList.FirstOrDefault(p => p.Name == Properties.Settings.Default.SelectedProjectForBranchCreate);                            
                            comboBoxProjectList.ItemsSource = projectList;
                            if (selectedProject != null)
                            {
                                comboBoxProjectList.SelectedItem = selectedProject;
                            }
                        }
                    }
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
                    //buttonFind.Content = "Find";
                }
            }
        }

        private  void SetNewBranchName()
        {
            textBoxBranchName.Text = myIfsSvn.GetNewBranchName(selectedTag, (comboBoxProjectList.SelectedItem as SvnProject).Name);
        }

        private void comboBoxProjectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {                
                if (comboBoxProjectList.SelectedItem != null && 
                    comboBoxProjectList.SelectedItem is SvnProject)
                {
                    this.SetNewBranchName();
                    Properties.Settings.Default.SelectedProjectForBranchCreate = (comboBoxProjectList.SelectedItem as SvnProject).Name;
                }
            }
            catch (Exception)
            {         
            }
        }

        private void textBoxSelectedTag_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                this.SetNewBranchName();
            }
            catch (Exception)
            {
            }
        }

        private void buttonCreate_Click(object sender, RoutedEventArgs e)
        {
            Xceed.Wpf.Toolkit.MessageBox.Show(App.Current.MainWindow, "Not implemented Yet!", "Not implemented Yet!", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }
    }
}
