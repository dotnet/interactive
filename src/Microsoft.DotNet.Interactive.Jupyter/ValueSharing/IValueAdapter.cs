using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using System;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing
{
    internal interface IValueAdapter : IDisposable, 
        IKernelCommandToMessageHandler<SetValue>, 
        IKernelCommandToMessageHandler<RequestValue>,
        IKernelCommandToMessageHandler<RequestValueInfos>
    {
    }
}
