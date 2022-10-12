using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.ValueSharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    internal class DefaultKernelValueDeclarer : IKernelValueDeclarer
    {
        public bool TryGetValueDeclaration(ValueProduced valueProduced, string declareAsName, out KernelCommand command)
        {
            command = new SetValue(valueProduced.Value, declareAsName ?? valueProduced.Name, valueProduced.FormattedValue);
            return true;
        }
    }
}
