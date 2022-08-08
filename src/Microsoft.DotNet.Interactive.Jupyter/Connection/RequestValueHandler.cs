using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.ValueSharing;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal class RequestValueHandler
    {
        private readonly ISupportGetValue _getValueHandler;

        public RequestValueHandler(ISupportGetValue getValueHandler)
        {
            _getValueHandler = getValueHandler;
        }

        public async Task HandleRequestValueAsync(RequestValue command, ICommandExecutionContext context, CancellationToken token)
        {
            await Task.Run(() =>
            {
                if (_getValueHandler.TryGetValue(command.Name, out object value))
                {
                    if (value is { })
                    {
                        var valueType = value.GetType();
                        var formatter = Formatter.GetPreferredFormatterFor(valueType, command.MimeType);

                        using var writer = new StringWriter(CultureInfo.InvariantCulture);
                        formatter.Format(value, writer);
                        var formatted = new FormattedValue(command.MimeType, writer.ToString());
                        context.Publish(new ValueProduced(value, command.Name, formatted, command));
                    }
                    else
                    {
                        var formatted = new FormattedValue(command.MimeType, "null");
                        context.Publish(new ValueProduced(value, command.Name, formatted, command));
                    }
                    context.Publish(new CommandSucceeded(command));
                }
                else
                {
                    throw new InvalidOperationException($"Cannot find value named: {command.Name}");
                }
            }, token).ContinueWith((t) =>
            {
                if (t.IsFaulted) throw t.Exception;
            });
        }

        public async Task HandleRequestValueInfosAsync(RequestValueInfos command, ICommandExecutionContext context, CancellationToken token)
        {
            await Task.Run(() =>
            {
                context.Publish(new ValueInfosProduced(_getValueHandler.GetValueInfos(), command));
            }, token);
        }
    }
}
