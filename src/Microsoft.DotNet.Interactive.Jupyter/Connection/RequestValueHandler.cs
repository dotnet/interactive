using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.ValueSharing;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal class RequestValueHandler
    {
        private readonly ISupportGetValue _valueHandler;

        public RequestValueHandler(ISupportGetValue valueHandler)
        {
            _valueHandler = valueHandler;
        }

        public Task HandleRequestValueAsync(RequestValue command, ICommandExecutionContext context, CancellationToken token)
        {
            // TODO
            return Task.CompletedTask;
        }

        public Task HandleRequestValueInfosAsync(RequestValueInfos command, ICommandExecutionContext context, CancellationToken token)
        {
            // TODO
            return Task.CompletedTask;
        }
    }
}
