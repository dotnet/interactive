using Microsoft.DotNet.Interactive.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal interface IKernelCommandToMessageHandler<TCommand> where TCommand: KernelCommand
    {
        Task HandleCommandAsync(TCommand command, ICommandExecutionContext context, CancellationToken token);
    }
}
