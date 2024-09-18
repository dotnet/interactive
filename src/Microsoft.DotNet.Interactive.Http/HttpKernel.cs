// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.ValueSharing;
using Microsoft.DotNet.Interactive.Http.Parsing.Parsing;

namespace Microsoft.DotNet.Interactive.Http;

using Diagnostic = CodeAnalysis.Diagnostic;

public class HttpKernel :
    Kernel,
    IKernelCommandHandler<ClearValues>,
    IKernelCommandHandler<RequestValue>,
    IKernelCommandHandler<SendValue>,
    IKernelCommandHandler<SubmitCode>,
    IKernelCommandHandler<RequestDiagnostics>,
    IKernelCommandHandler<RequestValueInfos>
{
    internal const int DefaultResponseDelayThresholdInMilliseconds = 1000;
    internal const int DefaultContentByteLengthThreshold = 500_000;

    private readonly HttpClient _client;
    private readonly int _responseDelayThresholdInMilliseconds;
    private readonly long _contentByteLengthThreshold;

    private readonly Dictionary<string, object> _variables = new(StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    /// Gets or sets a timeout for HTTP requests that are issued using this <see cref="HttpKernel"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default value is <see cref="Timeout.InfiniteTimeSpan"/>
    /// </para>
    /// <para>
    /// If an <see cref="HttpClient"/> is not passed in the constructor, the specified <see cref="RequestTimeout"/>
    /// will be the only timeout that applies.
    /// </para>
    /// <para>
    /// If an <see cref="HttpClient"/> is passed in the constructor, the timeout specified in
    /// <see cref="HttpClient.Timeout"/> will not be overridden by the specified <see cref="RequestTimeout"/>. The
    /// shorter of the two timeouts will apply in such cases.
    /// </para>
    /// <para>
    /// Note that <see cref="HttpClient.Timeout"/> has a default value of <c>100</c> seconds. If you wish to control
    /// the timeout exclusively via <see cref="RequestTimeout"/>, either omit the <see cref="HttpClient"/> parameter in
    /// the constructor, or pass in an <see cref="HttpClient"/> with <see cref="HttpClient.Timeout"/> set to
    /// <see cref="Timeout.InfiniteTimeSpan"/>.
    /// </para>
    /// </remarks>
    public TimeSpan RequestTimeout { get; set; } = Timeout.InfiniteTimeSpan;

    static HttpKernel()
        => KernelCommandEnvelope.RegisterCommand<ClearValues>();

    public HttpKernel(
        string? name = null,
        HttpClient? client = null,
        int responseDelayThresholdInMilliseconds = DefaultResponseDelayThresholdInMilliseconds,
        int contentByteLengthThreshold = DefaultContentByteLengthThreshold) : base(name ?? "http")
    {
        KernelInfo.LanguageName = "HTTP";
        KernelInfo.DisplayName = $"{KernelInfo.LocalName} - HTTP Request";
        KernelInfo.Description = """
                                 This Kernel is able to execute http requests and display the results.
                                 """;

        _client = client ?? new HttpClient() { Timeout = Timeout.InfiniteTimeSpan };
        _responseDelayThresholdInMilliseconds = responseDelayThresholdInMilliseconds;
        _contentByteLengthThreshold = contentByteLengthThreshold;

        RegisterForDisposal(_client);
    }

    public Task HandleAsync(ClearValues command, KernelInvocationContext context)
    {
        _variables.Clear();
        return Task.CompletedTask;
    }

    Task IKernelCommandHandler<RequestValue>.HandleAsync(RequestValue command, KernelInvocationContext context)
    {
        if (_variables.TryGetValue(command.Name, out var value))
        {
            var valueProduced = new ValueProduced(
                value,
                command.Name,
                FormattedValue.CreateSingleFromObject(value, JsonFormatter.MimeType),
                command);
            context.Publish(valueProduced);
        }
        else
        {
            context.Fail(command, message: $"Value not found: {command.Name}");
        }

        return Task.CompletedTask;
    }

    Task IKernelCommandHandler<RequestValueInfos>.HandleAsync(RequestValueInfos command, KernelInvocationContext context)
    {
        var valueInfos = _variables.Select(v => new KernelValueInfo(v.Key, FormattedValue.CreateSingleFromObject(v.Value, PlainTextSummaryFormatter.MimeType))).ToArray();

        context.Publish(new ValueInfosProduced(valueInfos, command));

        return Task.CompletedTask;
    }

    async Task IKernelCommandHandler<SendValue>.HandleAsync(SendValue command, KernelInvocationContext context)
        => await SetValueAsync(command, context, SetValueAsync);

    private Task SetValueAsync(string valueName, object value, Type? declaredType = null)
    {
        _variables[valueName] = value;
        return Task.CompletedTask;
    }

    async Task IKernelCommandHandler<SubmitCode>.HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        var parseResult = HttpRequestParser.Parse(command.Code);
        var requestNodes = parseResult.SyntaxTree.RootNode.ChildNodes.OfType<HttpRequestNode>();

        if (command.Parameters.TryGetValue("Document", out var doc))
        {
            var parsedDoc = HttpRequestParser.Parse(doc);
            var lastSpan = parsedDoc.SyntaxTree.RootNode.ChildNodes
                .OfType<HttpRequestNode>()
                .FirstOrDefault(n => n.Text == requestNodes.Last().Text)?.Span;
            if (lastSpan != null)
            {
                var docVariableNodes = parsedDoc.SyntaxTree.RootNode.ChildNodes.OfType<HttpVariableDeclarationAndAssignmentNode>();
                var docVariableNames = docVariableNodes.Where(n => n.Span.Start < lastSpan?.Start).Select(n => n.DeclarationNode?.VariableName).ToHashSet();
                foreach (DeclaredVariable dv in parsedDoc.SyntaxTree.RootNode.TryGetDeclaredVariables(BindExpressionValues).declaredVariables.Values)
                {
                    if (docVariableNames.Contains(dv.Name))
                    {
                        _variables[dv.Name] = dv.Value;
                    }
                }
            }

        }
        else
        {
            foreach (DeclaredVariable dv in parseResult.SyntaxTree.RootNode.TryGetDeclaredVariables(BindExpressionValues).declaredVariables.Values)
            {
                _variables[dv.Name] = dv.Value;
            }
        }

        var httpBoundResults = new List<HttpBindingResult<HttpRequestMessage>>();
        var httpNamedBoundResults = new List<(HttpRequestNode requestNode, HttpBindingResult<HttpRequestMessage> bindingResult)>();

        foreach (var requestNode in requestNodes)
        {
            if (requestNode.IsNamedRequest)
            {
                var httpNamedBoundResult = requestNode.TryGetHttpRequestMessage(BindExpressionValues);

                httpNamedBoundResults.Add((requestNode, httpNamedBoundResult));

            }
            else
            {
                httpBoundResults.Add(requestNode.TryGetHttpRequestMessage(BindExpressionValues));
            }
        }


        var diagnostics = httpBoundResults.SelectMany(n => n.Diagnostics).Concat(httpNamedBoundResults.SelectMany(n => n.bindingResult.Diagnostics)).ToArray();

        PublishDiagnostics(context, command, diagnostics);

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            var message = string.Join(Environment.NewLine, diagnostics.Select(d => d.ToString()));
            context.Fail(command, message: message);
            return;
        }

        var requestMessages = httpBoundResults
                              .Where(r => r is { IsSuccessful: true, Value: not null })
                              .Select(r => r.Value!).ToArray();

        var namedRequestMessages = httpNamedBoundResults.Where(n => n.bindingResult.IsSuccessful && n.bindingResult.Value is not null).ToArray();

        try
        {
            foreach (var requestMessage in requestMessages)
            {
                await SendRequestAsync(requestMessage, command, context);
            }

            foreach (var (requestNode, bindingResult) in namedRequestMessages)
            {
                await SendRequestAsync(bindingResult.Value!, command, context, requestNode);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException)
        {
            context.Fail(command, message: ex.Message);
        }
    }

    private async Task SendRequestAsync(HttpRequestMessage requestMessage, KernelCommand command, KernelInvocationContext context, HttpRequestNode? requestNode = null)
    {
        var cancellationToken = context.CancellationToken;
        var isResponseAvailable = false;
        var semaphore = new SemaphoreSlim(1);
        string? valueId = null;

        try
        {
            await Task.WhenAll(SendRequestAndHandleResponseAsync(), HandleDelayedResponseAsync());
        }
        catch
        {
            ClearDisplayedValue();
            throw;
        }

        async Task HandleDelayedResponseAsync()
        {
            await Task.Delay(_responseDelayThresholdInMilliseconds, cancellationToken);

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!isResponseAvailable)
                {
                    var emptyResponse = new EmptyHttpResponse();
                    UpdateDisplayedValue(emptyResponse);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        async Task SendRequestAndHandleResponseAsync()
        {
            var response = await GetResponseWithTimingAsync(requestMessage, cancellationToken);

            if (requestNode is not null)
            {
                var namedRequest = new HttpNamedRequest(requestNode, response);
                if (namedRequest.Name is not null)
                {
                    _variables[namedRequest.Name] = namedRequest;
                }

            }

            await semaphore.WaitAsync(cancellationToken);
            isResponseAvailable = true;
            semaphore.Release();

            var contentLength = response.Content?.ByteLength ?? 0;
            if (contentLength >= _contentByteLengthThreshold)
            {
                var partialResponse = response.ToPartialHttpResponse();
                UpdateDisplayedValue(partialResponse);
            }

            UpdateDisplayedValue(response);

            var jsonFormattedResponse =
                FormattedValue.CreateSingleFromObject(response, JsonFormatter.MimeType);

            jsonFormattedResponse.SuppressDisplay = true;

            context.Publish(
                new ReturnValueProduced(
                    response,
                    command,
                    formattedValues: new[] { jsonFormattedResponse }));
        }

        void UpdateDisplayedValue(object response)
        {
            var formattedValues = FormattedValue.CreateManyFromObject(response);

            if (string.IsNullOrEmpty(valueId))
            {
                valueId = Guid.NewGuid().ToString();
                context.Publish(new DisplayedValueProduced(response, command, formattedValues, valueId));
            }
            else
            {
                context.Publish(new DisplayedValueUpdated(response, valueId, command, formattedValues));
            }
        }

        void ClearDisplayedValue()
        {
            if (!string.IsNullOrEmpty(valueId))
            {
                var htmlFormattedValue = new FormattedValue(HtmlFormatter.MimeType, "<span/>");
                var plainTextFormattedValue = new FormattedValue(PlainTextFormatter.MimeType, string.Empty);
                var formattedValues = new[] { htmlFormattedValue, plainTextFormattedValue };
                context.Publish(new DisplayedValueUpdated(value: null, valueId, command, formattedValues));
            }
        }
    }

    private async Task<HttpResponse> GetResponseWithTimingAsync(
        HttpRequestMessage requestMessage,
        CancellationToken cancellationToken)
    {
        HttpResponse response;
        var stopWatch = Stopwatch.StartNew();
        var originalActivity = Activity.Current;

        using var timeoutSource = new CancellationTokenSource();
        var timeoutToken = timeoutSource.Token;
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken);
        var linkedToken = linkedSource.Token;

        try
        {
            // null out the current activity so the new one that we create won't be parented to it.
            Activity.Current = null;

            Activity.Current = new Activity("").Start();

            timeoutSource.CancelAfter(RequestTimeout);

            var responseMessage = await _client.SendAsync(requestMessage, linkedToken);
            response = (await responseMessage.ToHttpResponseAsync(linkedToken))!;
        }
        catch (OperationCanceledException ex) when (timeoutToken.IsCancellationRequested)
        {
            var message = string.Format(Resources.RequestTimedOut, RequestTimeout.TotalSeconds);
            throw new TaskCanceledException(message, ex);
        }
        finally
        {
            // restore the original activity
            Activity.Current = originalActivity;
            stopWatch.Stop();
        }

        response.ElapsedMilliseconds = stopWatch.Elapsed.TotalMilliseconds;
        return response;
    }

    private void PublishDiagnostics(KernelInvocationContext context, KernelCommand command, IReadOnlyCollection<Diagnostic> diagnostics)
    {
        var formattedDiagnostics =
            diagnostics
                .Select(d => d.ToString())
                .Select(text => new FormattedValue(PlainTextFormatter.MimeType, text))
                .ToArray();

        context.Publish(
            new DiagnosticsProduced(
                diagnostics.Select(ToSerializedDiagnostic).ToArray(),
                formattedDiagnostics,
                command));

        static Interactive.Diagnostic ToSerializedDiagnostic(Diagnostic d)
        {
            var lineSpan = d.Location.GetLineSpan();
            var start = new LinePosition(lineSpan.StartLinePosition.Line, lineSpan.StartLinePosition.Character);
            var end = new LinePosition(lineSpan.EndLinePosition.Line, lineSpan.EndLinePosition.Character);

            return new Interactive.Diagnostic(
                new LinePositionSpan(start, end),
                d.Severity,
                code: d.Id,
                message: d.GetMessage());
        }
    }

    Task IKernelCommandHandler<RequestDiagnostics>.HandleAsync(RequestDiagnostics command, KernelInvocationContext context)
    {
        var parseResult = HttpRequestParser.Parse(command.Code);
        var diagnostics = GetAllDiagnostics(parseResult);
        PublishDiagnostics(context, command, diagnostics);
        return Task.CompletedTask;
    }

    private List<Diagnostic> GetAllDiagnostics(HttpRequestParseResult parseResult)
    {
        var diagnostics = new List<Diagnostic>();

        foreach (var diagnostic in parseResult.GetDiagnostics())
        {
            diagnostics.Add(diagnostic);
        }

        foreach (var expressionNode in parseResult.SyntaxTree.RootNode.DescendantNodesAndTokensAndSelf().OfType<HttpExpressionNode>())
        {
            if (BindExpressionValues(expressionNode) is { IsSuccessful: false } bindResult)
            {
                diagnostics.AddRange(bindResult.Diagnostics);
            }
        }

        return diagnostics;
    }

    private HttpBindingResult<object?> BindExpressionValues(HttpExpressionNode node)
    {
        var expression = node.Text;
        string[] expressionPath = [];
        var expressionPathStart = "";


        if (expression.Contains('.'))
        {
            expressionPath = expression.Split('.');
            expressionPathStart = expressionPath.First();
        }

        if (_variables.TryGetValue(expression, out var value))
        {
            return node.CreateBindingSuccess(value);
        }
        else if (expressionPath.Length > 0 && _variables.TryGetValue(expressionPathStart, out var namedRequest) && namedRequest is HttpNamedRequest nr)
        {
            return nr.ResolvePath(expressionPath, node);
        }
        else
        {
            return DynamicExpressionUtilities.ResolveExpressionBinding(node, expression);
        }
    }
}