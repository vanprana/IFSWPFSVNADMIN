using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpSvn;

namespace IfsSvnClient.Classes
{
    internal class TagArguments
    {
        internal JobType Type { get; set; }
        internal SvnListEventArgs SelectedTag { get; set; }
        internal string BranchName { get; set; }

        internal TagArguments(JobType type)
        {
            this.Type = type;
        }
    }
}
