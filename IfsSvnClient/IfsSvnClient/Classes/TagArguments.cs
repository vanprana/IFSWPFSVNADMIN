using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IfsSvnClient.Classes
{
    internal class TagArguments
    {
        internal JobType Type { get; set; }

        internal TagArguments(JobType type)
        {
            this.Type = type;
        }
    }
}
