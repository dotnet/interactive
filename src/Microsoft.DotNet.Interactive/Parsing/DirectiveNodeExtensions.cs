// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Parsing;

internal static class DirectiveNodeExtensions
{
    public static async Task<ExpressionBindingResult> RequestAllInputsAndKernelValues(
        this DirectiveNode directiveNode,
        Kernel kernel)
    {
        Dictionary<string, InputProduced> inputsProduced = null;
        Dictionary<string, ValueProduced> valuesProduced = null;

        var (boundValues, diagnostics) =
            await directiveNode.TryBindExpressionsAsync(
                async expressionNode =>
                {
                    var (bindingResult, valueProduced, inputProduced) =
                        await kernel.RootKernel.SubmissionParser.RequestSingleValueOrInputAsync(
                            expressionNode,
                            directiveNode.TargetKernelName);

                    var parameterName = ((DirectiveParameterNode)expressionNode.Parent.Parent).NameNode.Text;

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

                    return bindingResult;
                });

        return new()
        {
            BoundValues = boundValues,
            Diagnostics = diagnostics,
            InputsProduced = inputsProduced,
            ValuesProduced = valuesProduced
        };
    }
}