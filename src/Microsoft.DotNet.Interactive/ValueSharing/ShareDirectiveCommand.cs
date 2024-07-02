// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.ValueSharing;

internal class ShareDirectiveCommand : KernelCommand
{
    [JsonPropertyName("as")] public string ImportValueAsName { get; set; }

    public string Name { get; set; }

    [JsonPropertyName("from")] public string SourceKernelName { get; set; }

    public string MimeType { get; set; }

    public static async Task HandleAsync(ShareDirectiveCommand command, KernelInvocationContext context)
    {
        var toKernel = context.HandlingKernel;
        var fromKernel = toKernel.RootKernel.FindKernelByName(command.SourceKernelName);
        var fromName = command.Name;
        var toName = command.ImportValueAsName;
        var requestedMimeType = command.MimeType;

        var supportedRequestValue = fromKernel.SupportsCommandType(typeof(RequestValue));

        if (!supportedRequestValue)
        {
            throw new InvalidOperationException($"Kernel {fromKernel} does not support command {nameof(RequestValue)}");
        }

        var requestValue = new RequestValue(fromName, mimeType: requestedMimeType);

        requestValue.SetParent(context.Command, true);

        var requestValueResult = await fromKernel.SendAsync(requestValue);

        switch (requestValueResult.Events[^1])
        {
            case CommandSucceeded:
                var valueProduced = requestValueResult.Events.OfType<ValueProduced>().SingleOrDefault();

                if (valueProduced is not null)
                {
                    var declarationName = toName ?? fromName;

                    bool ignoreReferenceValue = requestedMimeType is not null;

                    if (toKernel.SupportsCommandType(typeof(SendValue)))
                    {
                        var value =
                            ignoreReferenceValue
                                ? null
                                : valueProduced.Value;

                        await KernelExtensions.SendValue(context, toKernel, value, valueProduced.FormattedValue, declarationName);
                    }
                    else
                    {
                        throw new CommandNotSupportedException(typeof(SendValue), toKernel);
                    }
                }

                break;

            case CommandFailed:

                break;
        }
    }
}