// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;
using static Microsoft.DotNet.Interactive.Directives.ChooseKeyValueStoreKernelDirective;
using SyntaxNode = Microsoft.DotNet.Interactive.Parsing.SyntaxNode;

namespace Microsoft.DotNet.Interactive;

public class KeyValueStoreKernel :
    Kernel,
    IKernelCommandHandler<RequestValueInfos>,
    IKernelCommandHandler<RequestValue>,
    IKernelCommandHandler<SendValue>,
    IKernelCommandHandler<SubmitCode>
{
    internal const string DefaultKernelName = "value";

    private readonly ConcurrentDictionary<string, FormattedValue> _values = new();
    private ChooseKeyValueStoreKernelDirective _chooseKernelDirective;
    private (bool hadValue, FormattedValue previousValue, string newValue)? _lastOperation;

    public KeyValueStoreKernel(string name = DefaultKernelName) : base(name)
    {
        KernelInfo.DisplayName = $"{KernelInfo.LocalName} - Raw Value Storage";
    }

    Task IKernelCommandHandler<RequestValueInfos>.HandleAsync(RequestValueInfos command, KernelInvocationContext context)
    {
        var valueInfos = _values.Select(e => new KernelValueInfo(e.Key, e.Value, typeName: e.Value.MimeType)).ToArray();
        context.Publish(new ValueInfosProduced(valueInfos, command));
        return Task.CompletedTask;
    }

    Task IKernelCommandHandler<RequestValue>.HandleAsync(RequestValue command, KernelInvocationContext context)
    {
        if (_values.TryGetValue(command.Name, out var value))
        {
            context.Publish(new ValueProduced(
                                null,
                                command.Name,
                                value,
                                command));
        }
        else
        {
            context.Fail(command, message: $"Value '{command.Name}' not found in kernel {Name}");
        }

        return Task.CompletedTask;
    }

    Task IKernelCommandHandler<SendValue>.HandleAsync(SendValue command, KernelInvocationContext context)
    {
        _values[command.Name] = command.FormattedValue;
        return Task.CompletedTask;
    }

    public override ChooseKernelDirective ChooseKernelDirective =>
        _chooseKernelDirective ??= new(this);

    public override KernelSpecifierDirective CreateKernelSpecifierDirective()
    {
        var directive = base.CreateKernelSpecifierDirective();

        directive.Parameters.Add(new("--name")
        {
            Required = true
        });
        directive.Parameters.Add(new("--from-url"));
        directive.Parameters.Add(new("--from-file"));
        directive.Parameters.Add(new("--from-value"));
        directive.Parameters.Add(new("--mime-type"));

        directive.TryGetKernelCommandAsync = DirectiveTryGetKernelCommandAsync;

        return directive;

        async Task<KernelCommand> DirectiveTryGetKernelCommandAsync(
            DirectiveNode directiveNode,
            ExpressionBindingResult expressionBindingResult,
            Kernel kernel)
        {
            var parameterValues = directiveNode
                                  .GetParameterValues(directive, expressionBindingResult.BoundValues)
                                  .ToDictionary(t => t.Name, t => (t.Value, t.ParameterNode));

            string name = null;
            string? fromUrl = null;
            string? fromFile = null;
            string? mimeType = null;

            if (parameterValues.TryGetValue("--mime-type", out var mimeTypeResult) && mimeTypeResult.Value is string mimeTypeValue)
            {
                mimeType = mimeTypeValue;
            }

            if (parameterValues.TryGetValue("--name", out var nameResult) && nameResult.Value is string nameResultValue)
            {
                name = nameResultValue;
            }

            string? cellContentValue = null;

            if (directiveNode.NextNode() is { } nextNode)
            {
                cellContentValue = nextNode.FullText;
            }

            if (parameterValues.TryGetValue("--from-url", out var fromUrlResult) && fromUrlResult.Value is string fromUrlValue)
            {
                fromUrl = fromUrlValue;

                if (cellContentValue is not null)
                {
                    AddDiagnostic(
                        "--from-url",
                        PolyglotSyntaxParser.ErrorCodes.FromUrlAndCellContentCannotBeUsedTogether,
                        "The --from-url option cannot be used in combination with a content submission.");

                    return null;
                }
            }

            if (parameterValues.TryGetValue("--from-file", out var fromFileResult) && fromFileResult.Value is string fromFileValue)
            {
                fromFile = fromFileValue;

                if (fromUrl is not null)
                {
                    AddDiagnostic(
                        "--from-file",
                        PolyglotSyntaxParser.ErrorCodes.FromUrlAndFromFileCannotBeUsedTogether,
                        "The --from-url and --from-file options cannot be used together.");

                    return null;
                }

                if (cellContentValue is not null)
                {
                    AddDiagnostic(
                        "--from-file",
                        PolyglotSyntaxParser.ErrorCodes.FromFileAndCellContentCannotBeUsedTogether,
                        "The --from-file option cannot be used in combination with a content submission.");

                    return null;
                }
            }

            string? inlineValue = null;

            if (parameterValues.TryGetValue("--from-value", out var fromValueResult) && fromValueResult.Value is string fromValueValue)
            {
                if (fromUrl is not null)
                {
                    AddDiagnostic(
                        "--from-value",
                        PolyglotSyntaxParser.ErrorCodes.FromUrlAndFromValueCannotBeUsedTogether,
                        "The --from-url and --from-value options cannot be used together.");

                    return null;
                }

                if (fromFile is not null)
                {
                    AddDiagnostic(
                        "--from-value",
                        PolyglotSyntaxParser.ErrorCodes.FromFileAndFromValueCannotBeUsedTogether,
                        "The --from-value and --from-file options cannot be used together.");

                    return null;
                }

                inlineValue = fromValueValue;
            }

            if (inlineValue is not null || cellContentValue is not null)
            {
                var formattedValue = new FormattedValue(mimeType ?? PlainTextFormatter.MimeType, inlineValue ?? cellContentValue);

                return new SendValue(name, null, formattedValue, targetKernelName: Name);
            }

            // FIX: (CreateKernelSpecifierDirective) --from-file and --from-url

            return null;

            void AddDiagnostic(string parameterName, string errorCode, string message)
            {
                var targetNode = (SyntaxNode)directiveNode
                                             .ChildNodes
                                             .OfType<DirectiveParameterNode>()
                                             .FirstOrDefault(node => node.NameNode?.Text == parameterName)
                                 ??
                                 directiveNode;

                var diagnostic = targetNode.CreateDiagnostic(new(errorCode, message, DiagnosticSeverity.Error));

                directiveNode.AddDiagnostic(diagnostic);
            }
        }
    }

    public IReadOnlyDictionary<string, FormattedValue> Values => _values;

    async Task IKernelCommandHandler<SubmitCode>.HandleAsync(
        SubmitCode command,
        KernelInvocationContext context)
    {
        var parseResult = command.KernelChooserParseResult;

        var value = command.SyntaxNode.Text.Trim();

        var options = ValueDirectiveOptions.Create(parseResult, _chooseKernelDirective);

        await StoreValueAsync(command, context, options, value);
    }

    internal override bool AcceptsUnknownDirectives => true;

    internal async Task TryStoreValueFromOptionsAsync(
        KernelInvocationContext context,
        ValueDirectiveOptions options)
    {
        var mimeType = options.MimeType;
        string newValue = null;
        var loadedFromOptions = false;

        if (options.FromFile is { } file)
        {
            newValue = await IOExtensions.ReadAllTextAsync(file.FullName);
            loadedFromOptions = true;
        }
        else if (options.FromUrl is { } uri)
        {
            var client = new HttpClient();
            var response = await client.GetAsync(uri, context.CancellationToken);
            newValue = await response.Content.ReadAsStringAsync();
            mimeType ??= response.Content.Headers?.ContentType?.MediaType;
            loadedFromOptions = true;
        }
        else if (options.FromValue is { } value)
        {
            newValue = value;
            loadedFromOptions = true;
        }

        if (loadedFromOptions)
        {
            var hadValue = _values.TryGetValue(options.Name, out var previousValue);

            _lastOperation = (hadValue, previousValue, newValue);

            await StoreValueAsync(newValue, options, context, mimeType: mimeType);
        }
        else
        {
            _lastOperation = null;
        }
    }

    private async Task StoreValueAsync(
        KernelCommand command,
        KernelInvocationContext context,
        ValueDirectiveOptions options,
        string value = null,
        string mimeType = null)
    {
        if (options.FromFile is { })
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                UndoSetValue();
                context.Fail(command,
                             message: "The --from-file option cannot be used in combination with a content submission.");
            }
        }
        else if (options.FromUrl is { })
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                UndoSetValue();
                context.Fail(command,
                             message: "The --from-url option cannot be used in combination with a content submission.");
            }
        }
        else
        {
            await StoreValueAsync(value, options, context, mimeType: mimeType);
        }

        _lastOperation = default;

        void UndoSetValue()
        {
            if (_lastOperation is not null)
            {
                if (_lastOperation?.hadValue == true)
                {
                    // restore value
                    _values[options.Name] = _lastOperation?.previousValue;
                }
                else
                {
                    // remove entry
                    _values.TryRemove(options.Name, out _);
                }

                _lastOperation = null;
            }
        }
    }

    private async Task StoreValueAsync(
        string value,
        ValueDirectiveOptions options,
        KernelInvocationContext context,
        string mimeType = null)
    {
        mimeType ??= (options.MimeType ?? PlainTextFormatter.MimeType);
        var shouldDisplayValue = !string.IsNullOrWhiteSpace(options.MimeType);
        await StoreValueAsync(options.Name, value, mimeType, shouldDisplayValue, context);
    }

    protected virtual Task StoreValueAsync(
        string key,
        string value,
        string mimeType,
        bool shouldDisplayValue,
        KernelInvocationContext context)
    {
        _values[key] = new FormattedValue(mimeType, value);

        if (shouldDisplayValue)
        {
            context.DisplayAs(value, mimeType);
        }

        return Task.CompletedTask;
    }
}