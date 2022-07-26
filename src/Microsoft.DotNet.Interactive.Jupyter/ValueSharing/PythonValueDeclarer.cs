using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.ValueSharing;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    internal class PythonValueDeclarer : IKernelValueDeclarer
    {
        public bool TryGetValueDeclaration(ValueProduced valueProduced, out KernelCommand command)
        {
            switch (valueProduced.Value)
            {
                case (string stringValue):
                    command = new SubmitCode($"{valueProduced.Name}=\"{stringValue}\"");
                    command.Properties.Add("silent", true);
                    return true;
                default:
                    command = null;
                    return false;
            }
        }
    }
}
