using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpSvn;

namespace IfsSvnAdmin.Classes
{
    internal class SvnComponent
    {
        internal string Name { get; set; }
        internal string Path { get; set; }

        internal SvnComponent(string infor)
        {
            string[] values = infor.Split(new string[] { "\t", " " }, StringSplitOptions.RemoveEmptyEntries);

            this.Path = values[0];
            this.Name = values[1];
        }

        internal SvnComponent(SvnListEventArgs component)
        {
            this.Path = component.Uri.AbsoluteUri;
            this.Name = component.Name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
