﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpSvn;

namespace IfsSvnAdmin.Classes
{
    internal class SearchArguments
    {
        internal SvnUriTarget RootUri { get; set; }
        internal SvnUriTarget ComponentListUri { get; set; }
        internal string SearchPattern { get; set; }

        internal SearchArguments(string rootUri, string pattern)
        {
            this.SearchPattern = pattern;

            //this.SearchPattern = this.SearchPattern.Replace(".", @"\.");
            //this.SearchPattern = this.SearchPattern.Replace("?", ".");
            this.SearchPattern = this.SearchPattern.Replace("*", ".*?");
            //this.SearchPattern = this.SearchPattern.Replace(@"\", @"\\");
            //this.SearchPattern = this.SearchPattern.Replace(" ", @"\s");

            this.RootUri = new SvnUriTarget(rootUri);
            this.ComponentListUri = new SvnUriTarget(rootUri + @"/applications");
        }
    }
}