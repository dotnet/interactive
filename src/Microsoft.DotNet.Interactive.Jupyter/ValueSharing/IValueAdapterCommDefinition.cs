using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    internal interface IValueAdapterCommDefinition
    {
        string GetTargetDefinition(string targetName);
    }
}
