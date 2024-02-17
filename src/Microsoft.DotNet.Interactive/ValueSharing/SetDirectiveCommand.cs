// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.ValueSharing;

internal class ShareDirectiveCommand : KernelCommand
{
    public string Name { get; set; }
    public string As { get; set; }
    public string From { get; set; }

    public string MimeType { get; set; }

    public static async Task HandleAsync(ShareDirectiveCommand arg1, KernelInvocationContext arg2)
    {
    }
}

internal class SetDirectiveCommand : KernelCommand
{
    public string Name { get; set; }

    public string Value { get; set; }

    [JsonPropertyName("byref")] public bool ShareByRef { get; set; }

    public string MimeType { get; set; }

    internal static async Task HandleAsync(KernelCommand command, KernelInvocationContext context)
    {
        var kernel = context.HandlingKernel;
        var setCommand = (SetDirectiveCommand)command;

        if (kernel.SupportsCommandType(typeof(SendValue)))
        {
            var valueProducedEvents = new List<ValueProduced>();

            var inputProducedEvents = new List<InputProduced>();

            using var subscription = context.KernelEvents
                                            .Where(e => e is ValueProduced or InputProduced)
                                            .Subscribe(
                                                e =>
                                                {
                                                    switch (e)
                                                    {
                                                        case ValueProduced vp:
                                                            valueProducedEvents.Add(vp);
                                                            break;
                                                        case InputProduced ip:
                                                            inputProducedEvents.Add(ip);
                                                            break;
                                                    }
                                                });
            string sourceKernelName = null;

            var sourceKernel = Kernel.Root.FindKernelByName(sourceKernelName);

            ValueProduced valueProduced = null;

            if (sourceKernel is not null)
            {
                if (sourceKernel.KernelInfo.IsProxy)
                {
                    var destinationUri = sourceKernel.KernelInfo.RemoteUri;

                    valueProduced = valueProducedEvents.SingleOrDefault(e =>
                                                                            e.Name == setCommand.Name && e.Command.DestinationUri == destinationUri);
                }
                else
                {
                    valueProduced = valueProducedEvents.SingleOrDefault(e =>
                                                                            e.Name == setCommand.Name && e.Command.TargetKernelName == sourceKernelName);
                }
            }

            if (valueProduced is not null)
            {
                var referenceValue = setCommand.ShareByRef
                                         ? valueProduced.Value
                                         : null;
                var formattedValue = valueProduced.FormattedValue;

                await SendValue(context, kernel, referenceValue, formattedValue, setCommand.Name);
            }

            if (inputProducedEvents.Count > 0)
            {
                foreach (var inputProduced in inputProducedEvents)
                {
                    if (inputProduced.Command is RequestInput requestInput)
                    {
                        if (requestInput.IsPassword)
                        {
                            await SendValue(context, kernel, new PasswordString(inputProduced.Value), null, requestInput.ValueName);
                        }
                        else
                        {
                            await SendValue(context, kernel, inputProduced.Value, null, requestInput.ValueName);
                        }
                    }
                }
            }

            if (sourceKernelName is null)
            {
                if (inputProducedEvents.All(e => ((RequestInput)e.Command).ValueName != setCommand.Name))
                {
                    await SendValue(context, kernel, setCommand.Value, null, setCommand.Name);
                }
            }
        }
        else
        {
            context.Fail(context.Command, new CommandNotSupportedException(typeof(SendValue), kernel));
        }
    }

    private static async Task SendValue(
        KernelInvocationContext context,
        Kernel kernel,
        object value,
        FormattedValue formattedValue,
        string declarationName)
    {
        if (kernel.SupportsCommandType(typeof(SendValue)))
        {
            var sendValue = new SendValue(
                declarationName,
                value,
                formattedValue);

            sendValue.SetParent(context.Command, true);

            await kernel.SendAsync(sendValue);
        }
        else
        {
            throw new CommandNotSupportedException(typeof(SendValue), kernel);
        }
    }
}