using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IfsSvnAdmin.Classes
{
    internal class CheckOutArguments
    {
        internal CheckOutType Type { get; set; }
        internal SvnComponent[] CompornentArray { get; set; }
        internal string CheckOutPath { get; set; }
        internal Uri ProjectUri { get; set; }

        internal CheckOutArguments(CheckOutType type)
        {
            this.Type = type;
        }

        internal CheckOutArguments(CheckOutType type, Uri projectUri, string checkOutPath, SvnComponent[] componentArray)
        {
            this.Type = type;
            this.ProjectUri = projectUri;
            this.CheckOutPath = checkOutPath;
            this.CompornentArray = componentArray;
        }
    }

    internal enum CheckOutType
    {
        CheckOut,
        Load
    }
}
