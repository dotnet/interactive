// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;
using SyntaxNode = Microsoft.DotNet.Interactive.Parsing.SyntaxNode;

namespace Microsoft.DotNet.Interactive;

public class KeyValueStoreKernel :
    Kernel,
    IKernelCommandHandler<RequestValueInfos>,
    IKernelCommandHandler<RequestValue>,
    IKernelCommandHandler<SendValue>,
    IKernelCommandHandler<SubmitCode>
{
    private readonly HttpClient _httpClient;
    internal const string DefaultKernelName = "value";

    private readonly ConcurrentDictionary<string, FormattedValue> _values = new();

    public KeyValueStoreKernel(string name = DefaultKernelName, HttpClient httpClient = null) : base(name)
    {
        _httpClient = httpClient;
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

        context.Publish(new DisplayedValueProduced(command.FormattedValue.Value, command, [command.FormattedValue]));

        return Task.CompletedTask;
    }

    public override KernelSpecifierDirective KernelSpecifierDirective
    {
        get
        {
            var directive = base.KernelSpecifierDirective;

            directive.Parameters.Add(new("--name")
            {
                Required = true
            });
            directive.Parameters.Add(new("--from-url"));
            directive.Parameters.Add(new("--from-file")
            {
                TypeHint = "file"
            });
            directive.Parameters.Add(new("--from-value"));
            directive.Parameters.Add(new("--mime-type"));

            directive.TryGetKernelCommandAsync = TryGetKernelCommandAsync;

            return directive;

            Task<KernelCommand> TryGetKernelCommandAsync(
                DirectiveNode directiveNode,
                ExpressionBindingResult expressionBindingResult,
                Kernel keyValueStoreKernel)
            {
                if (expressionBindingResult.Diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error) is { } diagnostic)
                {
                    directiveNode.AddDiagnostic(diagnostic);
                    return Task.FromResult<KernelCommand>(null);
                }

                var parameterValues = directiveNode
                                      .GetParameterValues(expressionBindingResult.BoundValues)
                                      .ToDictionary(t => t.Name, t => (t.Value, t.ParameterNode));

                string name = null;
                string fromUrl = null;
                string fromFile = null;
                string mimeType = null;

                if (parameterValues.TryGetValue("--mime-type", out var mimeTypeResult) && mimeTypeResult.Value is string mimeTypeValue)
                {
                    mimeType = mimeTypeValue;
                }

                if (parameterValues.TryGetValue("--name", out var nameResult) && nameResult.Value is string nameResultValue)
                {
                    name = nameResultValue;
                }

                if (parameterValues.TryGetValue("--from-url", out var fromUrlResult) && fromUrlResult.Value is string fromUrlValue)
                {
                    fromUrl = fromUrlValue;
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

                        return Task.FromResult<KernelCommand>(null);
                    }
                }

                string inlineValue = null;

                if (parameterValues.TryGetValue("--from-value", out var fromValueResult) && fromValueResult.Value is string fromValueValue)
                {
                    if (fromUrl is not null)
                    {
                        AddDiagnostic(
                            "--from-value",
                            PolyglotSyntaxParser.ErrorCodes.FromUrlAndFromValueCannotBeUsedTogether,
                            "The --from-url and --from-value options cannot be used together.");

                        return Task.FromResult<KernelCommand>(null);
                    }

                    if (fromFile is not null)
                    {
                        AddDiagnostic(
                            "--from-value",
                            PolyglotSyntaxParser.ErrorCodes.FromFileAndFromValueCannotBeUsedTogether,
                            "The --from-value and --from-file options cannot be used together.");

                        return Task.FromResult<KernelCommand>(null);
                    }

                    inlineValue = fromValueValue;
                }

                var cellContent = GetCellContent();

                if (fromFile is not null)
                {
                    if (cellContent is not null)
                    {
                        AddDiagnostic("--from-file",
                                      PolyglotSyntaxParser.ErrorCodes.FromFileAndCellContentCannotBeUsedTogether,
                                      "The --from-file option cannot be used in combination with a content submission.");
                        return Task.FromResult<KernelCommand>(null);
                    }
                }
                else if (fromUrl is not null)
                {
                    if (cellContent is not null)
                    {
                        AddDiagnostic("--from-url",
                                      PolyglotSyntaxParser.ErrorCodes.FromUrlAndCellContentCannotBeUsedTogether,
                                      "The --from-url option cannot be used in combination with a content submission.");
                        return Task.FromResult<KernelCommand>(null);
                    }
                }

                if (fromFile is not null || fromUrl is not null)
                {
                    return Task.FromResult<KernelCommand>(new AnonymousKernelCommand(async (_, context) =>
                    {
                        string valueToStore = null;

                        if (fromFile is not null)
                        {
                            valueToStore = await GetValueFromFileAsync();
                        }
                        else if (fromUrl is not null)
                        {
                            (valueToStore, var responseMimeType) = await GetValueFromUrlAsync(
                                                                   fromUrl,
                                                                   context.CancellationToken);
                            if (mimeType is null)
                            {
                                mimeType = responseMimeType;
                            }
                        }

                        var formattedValue = new FormattedValue(mimeType ?? PlainTextFormatter.MimeType, valueToStore);

                        var sendValue = new SendValue(name, null, formattedValue, targetKernelName: Name);

                        await keyValueStoreKernel.SendAsync(sendValue);

                        async Task<string> GetValueFromFileAsync()
                        {
                            return await IOExtensions.ReadAllTextAsync(fromFile);
                        }

                    }));
                }

                var valueToStore = inlineValue ?? cellContent;

                if (valueToStore is not null)
                {
                    var formattedValue = new FormattedValue(mimeType ?? PlainTextFormatter.MimeType, valueToStore);

                    return Task.FromResult<KernelCommand>(new SendValue(name, null, formattedValue, targetKernelName: Name));
                }

                return Task.FromResult<KernelCommand>(null);

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

                string GetCellContent()
                {
                    if (directiveNode.NextNode() is DirectiveNode { NameNode.Text: "#!value" })
                    {
                        return null;
                    }

                    return directiveNode.NextNode()?.FullText;
                }
            }
        }
    }

    public IReadOnlyDictionary<string, FormattedValue> Values => _values;

    Task IKernelCommandHandler<SubmitCode>.HandleAsync(
        SubmitCode command,
        KernelInvocationContext context)
    {
        return Task.CompletedTask;
    }

    internal override bool AcceptsUnknownDirectives => true;

    private async Task<(string content, string mimeType)> GetValueFromUrlAsync(
        string fromUrl,
        CancellationToken cancellationToken)
    {
        var client = _httpClient ?? new HttpClient();
        var response = await client.GetAsync(fromUrl, cancellationToken);
        var mimeType = response.Content.Headers.ContentType?.MediaType;

#if NETSTANDARD2_0
        return (await response.Content.ReadAsStringAsync(), mimeType);
#else
        return (await response.Content.ReadAsStringAsync(cancellationToken), mimeType);
#endif
    }
}