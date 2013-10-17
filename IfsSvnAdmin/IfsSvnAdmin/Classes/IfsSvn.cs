using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpSvn;
using SharpSvn.UI;
using System.Collections.ObjectModel;

namespace IfsSvnAdmin.Classes
{
    internal class IfsSvn
    {
        private readonly SvnUriTarget componentsUri = new SvnUriTarget(Properties.Settings.Default.ServerUri + "/applications");
        private readonly SvnUriTarget projectsUri = new SvnUriTarget(Properties.Settings.Default.ServerUri + "/projects");

        internal List<SvnListEventArgs> GetProjectList()
        {
            try
            {
                return this.GetFolderList(this.projectsUri);
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal List<SvnListEventArgs> GetComponentList()
        {
            try
            {
                return this.GetFolderList(this.componentsUri);
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal List<SvnListEventArgs> GetFolderList(SvnUriTarget targetUri)
        {
            try
            {
                List<SvnListEventArgs> folderList = new List<SvnListEventArgs>();

                using (SvnClient client = new SvnClient())
                {
                    // Bind the SharpSvn UI to our client for SSL certificate and credentials
                    SvnUIBindArgs bindArgs = new SvnUIBindArgs();
                    SvnUI.Bind(client, bindArgs);
                    
                    Collection<SvnListEventArgs> itemList;
                    if (client.GetList(targetUri, out itemList))
                    {
                        foreach (SvnListEventArgs component in itemList)
                        {
                            if (component.Entry.NodeKind == SvnNodeKind.Directory)
                            {
                                folderList.Add(component);
                            }
                        }
                    }
                }

                return folderList;
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal string GetNewBranchName(SvnListEventArgs selectedTag, string projectName)
        {
            try
            {
                int count = 1;
                string proposeName = string.Empty;
                if (selectedTag != null)
                {
                    proposeName = selectedTag.Name + "." + count + "_" + projectName + "_dev";

                    string relativeComponentPath = string.Join(string.Empty, selectedTag.BaseUri.Segments.Take(selectedTag.BaseUri.Segments.Count() - 1).ToArray());
                    string server = selectedTag.BaseUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped);

                    SvnUriTarget barnchUri = new SvnUriTarget(server + relativeComponentPath + "branches/project");

                    List<SvnListEventArgs> branchList = this.GetFolderList(barnchUri);

                    bool newNameNotfound = true;
                    SvnListEventArgs foundBranch;
                    while (newNameNotfound)
                    {
                        foundBranch = branchList.FirstOrDefault(b => b.Name == proposeName);
                        if (foundBranch == null)
                        {
                            newNameNotfound = false;
                        }
                        else
                        {
                            count++;
                            proposeName = selectedTag.Name + "." + count + "_" + projectName + "_dev";
                        }
                    }
                }
                return proposeName;
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal List<SvnComponent> GetComponentListFromExternals(SvnProject seletedProject)
        {
            try
            {
                List<SvnComponent> componentList = new List<SvnComponent>();
                using (SvnClient client = new SvnClient())
                {
                    // Bind the SharpSvn UI to our client for SSL certificate and credentials
                    SvnUIBindArgs bindArgs = new SvnUIBindArgs();
                    SvnUI.Bind(client, bindArgs);

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
                            componentList.Add(component);
                        }
                    }
                }
                return componentList;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
