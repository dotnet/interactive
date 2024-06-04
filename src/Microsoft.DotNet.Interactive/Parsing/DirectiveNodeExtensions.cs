// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Parsing;

internal static class DirectiveNodeExtensions
{
    public static async Task<ExpressionBindingResult> RequestAllInputsAndKernelValues(
        this DirectiveNode directiveNode,
        Kernel kernel)
    {
        Dictionary<string, InputProduced>? inputsProduced = null;
        Dictionary<string, ValueProduced>? valuesProduced = null;

        var (boundValues, diagnostics) =
            await directiveNode.TryBindExpressionsAsync(
                async expressionNode =>
                {
                    var (bindingResult, valueProduced, inputProduced) =
                        await kernel.RootKernel.SubmissionParser.RequestSingleValueOrInputAsync(
                            expressionNode,
                            directiveNode.TargetKernelName);

                    if (expressionNode.Parent is { Parent: DirectiveParameterNode { NameNode.Text: { } parameterName } })
                    {
                        if (inputProduced is not null)
                        {
                            inputsProduced ??= new();
                            inputsProduced.Add(parameterName, inputProduced);
                        }

                        if (valueProduced is not null)
                        {
                            valuesProduced ??= new();
                            valuesProduced.Add(parameterName, valueProduced);
                        }
                    }

                    return bindingResult;
                });

        var result = new ExpressionBindingResult
        {
            BoundValues = boundValues,
            Diagnostics = diagnostics,
        };

        if (inputsProduced is not null)
        {
            result.InputsProduced.MergeWith(inputsProduced);
        }

        if (valuesProduced is not null)
        {
            result.ValuesProduced.MergeWith(valuesProduced);
        }

        return result;
    }
}