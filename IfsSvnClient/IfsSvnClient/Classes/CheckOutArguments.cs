using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IfsSvnClient.Classes
{
    internal class CheckOutArguments
    {
        internal JobType Type { get; set; }
        internal SvnComponent[] CompornentArray { get; set; }
        internal string CheckOutPathProject { get; set; }
        internal string CheckOutPathNbproject { get; set; }
        internal string CheckOutPathDocument { get; private set; }
        internal string CheckOutPathWorkspace { get; private set; }
        internal Uri ProjectNbprojectUri { get; private set; }
        internal Uri ProjectWorkspaceUri { get; private set; }
        internal Uri ProjectDocumentUri { get; private set; }
        internal bool ShowLessLogInformation { get; private set; }

        internal CheckOutArguments(JobType type)
        {
            this.Type = type;
        }

        internal CheckOutArguments(JobType type, bool showLessLogInformation, string projectPath, string checkOutPathProject, SvnComponent[] componentArray)
        {
            this.Type = type;
            this.ShowLessLogInformation = showLessLogInformation;
            this.CheckOutPathProject = checkOutPathProject;

            this.ProjectNbprojectUri = new Uri(projectPath + Properties.Settings.Default.ServerNbProject);
            this.ProjectDocumentUri = new Uri(projectPath + Properties.Settings.Default.ServerDocumentation);
            this.ProjectWorkspaceUri = new Uri(projectPath + Properties.Settings.Default.ServerWorkSpace);

            this.CheckOutPathNbproject = this.CheckOutPathProject;
            this.CheckOutPathDocument = this.CheckOutPathProject;
            this.CheckOutPathWorkspace = this.CheckOutPathProject;
            if (this.CheckOutPathProject.EndsWith(@"\"))
            {
                this.CheckOutPathNbproject += Properties.Settings.Default.ServerNbProject;
                this.CheckOutPathDocument += Properties.Settings.Default.ServerDocumentation;
                this.CheckOutPathWorkspace += Properties.Settings.Default.ServerWorkSpace;
            }
            else
            {
                this.CheckOutPathNbproject += @"\" + Properties.Settings.Default.ServerNbProject;
                this.CheckOutPathDocument += @"\" + Properties.Settings.Default.ServerDocumentation;
                this.CheckOutPathWorkspace += @"\" + Properties.Settings.Default.ServerWorkSpace;
            }
            this.CheckOutPathNbproject += @"\";
            this.CheckOutPathDocument += @"\";
            this.CheckOutPathWorkspace += @"\";

            this.CompornentArray = componentArray;
        }
    }
}
