using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpSvn;
using SharpSvn.UI;
using System.Collections.ObjectModel;

namespace IfsSvnClient.Classes
{
    internal class IfsSvn
    {
        private readonly SvnUriTarget componentsUri = new SvnUriTarget(Properties.Settings.Default.ServerUri + "/applications");
        private readonly SvnUriTarget projectsUri = new SvnUriTarget(Properties.Settings.Default.ServerUri + "/projects");

        private bool CheckUrlValide(SvnClient client, SvnUriTarget uri)
        {
            Collection<SvnInfoEventArgs> info;
            return client.GetInfo(uri, new SvnInfoArgs { ThrowOnError = false }, out info);
        }

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

                    if (this.CheckUrlValide(client, targetUri))
                    {
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
                    if (client.TryGetProperty(projectUri, SvnPropertyNames.SvnExternals, out components))
                    {
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
                }
                return componentList;
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal List<SvnComponent> GetDocumentComponentListFromExternals(SvnProject seletedProject)
        {
            try
            {
                List<SvnComponent> componentList = new List<SvnComponent>();
                using (SvnClient client = new SvnClient())
                {
                    // Bind the SharpSvn UI to our client for SSL certificate and credentials
                    SvnUIBindArgs bindArgs = new SvnUIBindArgs();
                    SvnUI.Bind(client, bindArgs);

                    SvnUriTarget projectUri = new SvnUriTarget(seletedProject.Path + Properties.Settings.Default.ServerDocumentation + "/");

                    string components;
                    if (client.TryGetProperty(projectUri, SvnPropertyNames.SvnExternals, out components))
                    {
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
                }
                return componentList;
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal bool CreateComponents(List<string> componentNameList)
        {
            try
            {
                using (SvnClient client = new SvnClient())
                {
                    // Bind the SharpSvn UI to our client for SSL certificate and credentials
                    SvnUIBindArgs bindArgs = new SvnUIBindArgs();
                    SvnUI.Bind(client, bindArgs);

                    List<Uri> folderList = new List<Uri>();

                    if (componentNameList.Count > 0)
                    {
                        foreach (string componentName in componentNameList)
                        {
                            SvnUriTarget componentUrl = new SvnUriTarget(this.componentsUri + "/" + componentName);

                            if (this.CheckUrlValide(client, componentUrl) == false)
                            {
                                folderList.AddRange(new Uri[] 
                                {
                                    componentUrl.Uri,
                                    new Uri(string.Format("{0}/branches/", componentUrl.ToString())),
                                    new Uri(string.Format("{0}/branches/archive", componentUrl.ToString())),
                                    new Uri(string.Format("{0}/branches/core", componentUrl.ToString())),
                                    new Uri(string.Format("{0}/branches/core/ifsapp-8.0", componentUrl.ToString())),
                                    new Uri(string.Format("{0}/branches/core/ifsapp-8.0/main", componentUrl.ToString())),
                                    new Uri(string.Format("{0}/branches/core/ifsapp-9.0", componentUrl.ToString())),
                                    new Uri(string.Format("{0}/branches/core/ifsapp-9.0/main", componentUrl.ToString())),
                                    new Uri(string.Format("{0}/branches/project", componentUrl.ToString())),
                                    new Uri(string.Format("{0}/tags/", componentUrl.ToString())),
                                    new Uri(string.Format("{0}/trunk/", componentUrl.ToString()))
                                });
                            }
                        }

                        SvnCreateDirectoryArgs arg = new SvnCreateDirectoryArgs();
                        arg.CreateParents = true;
                        arg.LogMessage = "ADMIN-0: Component structure created.";

                        return client.RemoteCreateDirectories(folderList, arg);
                    }
                }
                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal bool CreateProject(string projectName)
        {
            try
            {
                using (SvnClient client = new SvnClient())
                {
                    // Bind the SharpSvn UI to our client for SSL certificate and credentials
                    SvnUIBindArgs bindArgs = new SvnUIBindArgs();
                    SvnUI.Bind(client, bindArgs);

                    List<Uri> folderList = new List<Uri>();

                    if (string.IsNullOrWhiteSpace(projectName) == false)
                    {
                        SvnUriTarget projectUrl = new SvnUriTarget(this.projectsUri + "/" + projectName);

                        if (this.CheckUrlValide(client, projectsUri) == false)
                        {
                            folderList.AddRange(new Uri[] 
                            {
                               projectUrl.Uri,
                               new Uri(string.Format("{0}/documentation/", projectUrl.ToString())),
                               new Uri(string.Format("{0}/nbproject/", projectUrl.ToString())),
                               new Uri(string.Format("{0}/release/", projectUrl.ToString())),    
                               new Uri(string.Format("{0}/tags/", projectUrl.ToString())),   
                               new Uri(string.Format("{0}/workspace/", projectUrl.ToString()))
                            });
                        }

                        SvnCreateDirectoryArgs arg = new SvnCreateDirectoryArgs();
                        arg.CreateParents = true;
                        arg.LogMessage = "ADMIN-0: Component structure created.";

                        return client.RemoteCreateDirectories(folderList, arg);
                    }
                }
                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal bool CreateBranch(SvnListEventArgs selectedTag, string branchName)
        {
            try
            {
                if (selectedTag != null)
                {
                    using (SvnClient client = new SvnClient())
                    {
                        // Bind the SharpSvn UI to our client for SSL certificate and credentials
                        SvnUIBindArgs bindArgs = new SvnUIBindArgs();
                        SvnUI.Bind(client, bindArgs);

                        string relativeComponentPath = string.Join(string.Empty, selectedTag.BaseUri.Segments.Take(selectedTag.BaseUri.Segments.Count() - 1).ToArray());
                        string server = selectedTag.BaseUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped);

                        Uri barnchUri = new Uri(server + relativeComponentPath + "branches/project/" + branchName);

                        SvnTarget source = SvnTarget.FromUri(selectedTag.Uri);

                        SvnCopyArgs arg = new SvnCopyArgs();
                        arg.CreateParents = false;
                        arg.LogMessage = string.Format("ADMIN-0: Branch Created from Tag {0}", selectedTag.Name);
                        
                        return client.RemoteCopy(source, barnchUri, arg);
                    }
                }
                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
