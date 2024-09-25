// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Parsing;
using System.Linq;
using System.Threading.Tasks;

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

    public static Task<KernelCommand> TryParseSetDirectiveCommandAsync(
        DirectiveNode directiveNode,
        ExpressionBindingResult bindingResult,
        Kernel kernel)
    {
        if (!directiveNode.TryGetActionDirective(out var directive))
        {
            return Task.FromResult<KernelCommand>(null);
        }

        var command = new SetDirectiveCommand
        {
            TargetKernelName = directiveNode.TargetKernelName
        };

        if (bindingResult.Diagnostics.Length > 0)
        {
            return Task.FromResult<KernelCommand>(null);
        }

        var parameterValues = directiveNode
                              .GetParameterValues(
                                  directive,
                                  bindingResult.BoundValues)
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
                            LocalizationResources.Magics_set_mime_type_ErrorMessageCannotBeUsed(),
                            DiagnosticSeverity.Error)));
            }
        }

        if (parameterValues.TryGetValue("--name", out var destinationValueNameBinding))
        {
            command.DestinationValueName = (string)destinationValueNameBinding.Value;
        }

        if (bindingResult.InputsProduced.TryGetValue("--value", out var inputProduced))
        {
            // FIX: (TryParseSetDirectiveCommandAsync) can this be a BoundValue and remove InputsProduced property?
            if (((RequestInput)inputProduced.Command).IsPassword)
            {
                command.ReferenceValue = new PasswordString(inputProduced.Value);
            }
            else
            {
                command.ReferenceValue = inputProduced.Value;
            }
        }
        else if (bindingResult.ValuesProduced.TryGetValue("--value", out var valueProduced))
        {
            command.SourceValueName = valueProduced.Name;
            command.SourceKernelName = (valueProduced.Command as RequestValue)?.TargetKernelName;
            command.FormattedValue = valueProduced.FormattedValue;
            
            if (valueProduced.Value is not null && command.ShareByRef)
            {
                command.ReferenceValue = valueProduced.Value;
            }
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
                return Task.FromResult<KernelCommand>(null);
            }
        }

        return Task.FromResult<KernelCommand>(command);
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