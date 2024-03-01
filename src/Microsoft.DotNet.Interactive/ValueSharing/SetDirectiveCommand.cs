// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
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

        Dictionary<string, InputProduced> inputsProduced = null;
        Dictionary<string, ValueProduced> valuesProduced = null;

        var (boundExpressionValues, diagnostics) =
            await directiveNode.TryBindExpressionsAsync(
                async expressionNode =>
                {
                    var (bindingResult, valueProduced, inputProduced) =
                        await kernel.SubmissionParser.RequestSingleValueOrInputAsync(expressionNode, directiveNode.TargetKernelName);

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

            if (command.ShareByRef)
            {
                directiveNode.AddDiagnostic(
                    directiveNode.CreateDiagnostic(
                        new(PolyglotSyntaxParser.ErrorCodes.ByRefAndMimeTypeCannotBeCombined,
                            "The --mime-type and --byref options cannot be used together.", DiagnosticSeverity.Error)));
            }
        }

        if (parameterValues.TryGetValue("--name", out var destinationValueNameBinding))
        {
            command.DestinationValueName = (string)destinationValueNameBinding.Value;
        }

        if (inputsProduced?.TryGetValue("--value", out var inputProduced1) is true)
        {
            if (((RequestInput)inputProduced1.Command).IsPassword)
            {
                command.ReferenceValue = new PasswordString(inputProduced1.Value);
            }
            else
            {
                command.ReferenceValue = inputProduced1.Value;
            }
        }
        else if (valuesProduced?.TryGetValue("--value", out var valueProduced1) is true)
        {
            command.SourceValueName = valueProduced1.Name;
            command.SourceKernelName = (valueProduced1.Command as RequestValue)?.TargetKernelName;
            command.FormattedValue = valueProduced1.FormattedValue;
        }
        else if (parameterValues.TryGetValue("--value", out var parsedLiteralValueBinding))
        {
            command.ReferenceValue = parsedLiteralValueBinding.Value;
        }


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

    internal static async Task HandleAsync(SetDirectiveCommand command, KernelInvocationContext context)
    {
        var destinationKernel = context.HandlingKernel;

        if (destinationKernel.SupportsCommandType(typeof(SendValue)))
        {
            await SendValue(context,
                            destinationKernel,
                            command.ReferenceValue,
                            command.FormattedValue,
                            command.DestinationValueName);
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