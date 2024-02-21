// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.ValueSharing;

internal class SetDirectiveCommand : KernelCommand
{
    public string DestinationValueName { get; set; }

    public string SourceKernelName { get; set; }

    public string SourceValueName { get; set; }

    public string MimeType { get; set; }

    public bool ShareByRef { get; set; }

    public object ReferenceValue { get; set; }

    public FormattedValue FormattedValue { get; set; }

    public static async Task<KernelCommand> TryParseSetDirectiveCommand(
        DirectiveNode directiveNode,
        Kernel kernel)
    {
        if (!directiveNode.TryGetActionDirective(out var directive))
        {
            return null;
        }

        var command = new SetDirectiveCommand
        {
            TargetKernelName = directiveNode.TargetKernelName
        };

        List<ValueProduced> valuesProduced = null;

        var (boundExpressionValues, diagnostics) =
            await directiveNode.TryBindExpressionsAsync(
                async expressionNode =>
                {
                    var (bindingResult, valueProduced, inputProduced) = await kernel.SubmissionParser.RequestSingleValueOrInputAsync(expressionNode, directiveNode.TargetKernelName);

                    if (inputProduced is not null)
                    {

                    }

                    if (valueProduced is not null)
                    {
                        valuesProduced ??= new();
                        valuesProduced.Add(valueProduced);
                    }

                    return bindingResult;
                });

        if (diagnostics.Length > 0)
        {
            return null;
        }

        var parameterValues = directiveNode
                              .GetParameters(
                                  directive,
                                  boundExpressionValues)
                              .ToDictionary(t => t.Name, t => (t.Value, t.ParameterNode));

        if (parameterValues.TryGetValue("--byref", out var byRefBinding))
        {
            command.ShareByRef = (bool)byRefBinding.Value;
        }

        if (parameterValues.TryGetValue("--mime-type", out var mimeTypeBinding))
        {
            command.MimeType = (string)mimeTypeBinding.Value;
        }

        if (parameterValues.TryGetValue("--name", out var destinationValueNameBinding))
        {
            command.DestinationValueName = (string)destinationValueNameBinding.Value;
        }

        switch (valuesProduced)
        {
            case null:
                if (parameterValues.TryGetValue("--value", out var parsedLiteralValueBinding))
                {
                    command.ReferenceValue = parsedLiteralValueBinding.Value;

                }

                break;

            case [var singleValue]:
                command.SourceValueName = singleValue.Name;
                command.SourceKernelName = (singleValue.Command as RequestValue)?.TargetKernelName;
                command.FormattedValue = singleValue.FormattedValue;
                break;
        }

        // FIX: (TryParseDirectiveNode) bind

        // FIX: (TryParseDirectiveNode) validate

        var expressionNodes = directiveNode.DescendantNodesAndTokens().OfType<DirectiveExpressionNode>().ToArray();

        foreach (var expressionNode in expressionNodes)
        {
            if (command.ShareByRef && kernel.KernelInfo.IsProxy)
            {
                var diagnostic = expressionNode.CreateDiagnostic(
                    new(PolyglotSyntaxParser.ErrorCodes.ByRefNotSupportedWithProxyKernels,
                        LocalizationResources.Magics_set_ErrorMessageSharingByReference(),
                        DiagnosticSeverity.Error));
                directiveNode.AddDiagnostic(diagnostic);
                return null;
            }
        }

        return command;
    }

    internal static async Task HandleAsync(KernelCommand command, KernelInvocationContext context)
    {
        var destinationKernel = context.HandlingKernel;
        var setCommand = (SetDirectiveCommand)command;

        if (destinationKernel.SupportsCommandType(typeof(SendValue)))
        {
            await SendValue(context, destinationKernel, setCommand.ReferenceValue, setCommand.FormattedValue, setCommand.DestinationValueName);
        }
        else
        {
            context.Fail(context.Command, new CommandNotSupportedException(typeof(SendValue), destinationKernel));
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