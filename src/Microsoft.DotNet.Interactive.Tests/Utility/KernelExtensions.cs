// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public static class KernelExtensions
{
    public static async Task<(bool success, ValueInfosProduced valueInfosProduced)> TryRequestValueInfosAsync(this Kernel kernel)
    {
        if (kernel.SupportsCommandType(typeof(RequestValueInfos)))
        {
            var result = await kernel.SendAsync(new RequestValueInfos());

            var candidateResult = result.Events.OfType<ValueInfosProduced>().FirstOrDefault();
            if (candidateResult is not null)
            {
                return (true, candidateResult);
            }
        }

        return (false, default);
    }

    public static async Task<ValueProduced> RequestValueAsync(this Kernel kernel, string valueName)
    {
        var commandResult = await kernel.SendAsync(new RequestValue(valueName));

        commandResult.Events.Should().Contain(e => e is ValueProduced);

        return commandResult.Events.OfType<ValueProduced>().First();
    }

    public static void RespondToRequestInputsFormWith(
        this CompositeKernel kernel,
        Dictionary<string, string> formValues)
    {
        var subscription = kernel.KernelEvents.Subscribe(e =>
        {
            if (e is DisplayedValueProduced dvp)
            {
                // Grab the form id from the displayed value
                var formId = Regex.Match(
                    dvp.FormattedValues.Single().Value,
                    "form id=\"([a-zA-Z0-9]*)\"").Groups[1].Value;

                var sendValue = new SendValue(formId, formValues, FormattedValue.CreateSingleFromObject(formValues, "application/json"));

                Task.Run(async () => await kernel.SendAsync(sendValue));
            }
        });

        kernel.RegisterForDisposal(subscription);
    }
}