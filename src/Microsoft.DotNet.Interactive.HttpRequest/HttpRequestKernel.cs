// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.HttpRequest;

public class HttpRequestKernel :
       Kernel,
       IKernelCommandHandler<RequestValue>,
       IKernelCommandHandler<SendValue>,
       IKernelCommandHandler<SubmitCode>,
       IKernelCommandHandler<RequestDiagnostics>
{
    internal const int DefaultResponseDelayThresholdInMilliseconds = 1000;
    internal const int DefaultContentByteLengthThreshold = 500_000;

    private readonly HttpClient _client;
    private readonly int _responseDelayThresholdInMilliseconds;
    private readonly long _contentByteLengthThreshold;

    private readonly Dictionary<string, string> _variables = new(StringComparer.InvariantCultureIgnoreCase);
    private static readonly Regex IsRequest;
    private static readonly Regex IsHeader;
    private bool _useNewParser = true;

    private const string InterpolationStartMarker = "{{";
    private const string InterpolationEndMarker = "}}";

    static HttpRequestKernel()
    {
        // FIX: (HttpRequestKernel) delete me
        var verbs = string.Join("|",
            typeof(HttpMethod).GetProperties(BindingFlags.Static | BindingFlags.Public).Select(p => p.GetValue(null)!.ToString()));

        IsRequest = new Regex(@"^\s*(" + verbs + ")", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

        IsHeader = new Regex(@"^\s*(?<key>[\w-]+):\s*(?<value>.*)", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public HttpRequestKernel(
        string? name = null,
        HttpClient? client = null,
        int responseDelayThresholdInMilliseconds = DefaultResponseDelayThresholdInMilliseconds,
        int contentByteLengthThreshold = DefaultContentByteLengthThreshold) : base(name ?? "http")
    {
        KernelInfo.LanguageName = "HTTP";
        KernelInfo.DisplayName = $"{KernelInfo.LocalName} - HTTP Request";

        _client = client ?? new HttpClient();
        _responseDelayThresholdInMilliseconds = responseDelayThresholdInMilliseconds;
        _contentByteLengthThreshold = contentByteLengthThreshold;

        RegisterForDisposal(_client);
    }

    Task IKernelCommandHandler<RequestValue>.HandleAsync(RequestValue command, KernelInvocationContext context)
    {
        if (_variables.TryGetValue(command.Name, out var value))
        {
            var valueProduced = new ValueProduced(
                value,
                command.Name,
                FormattedValue.CreateSingleFromObject(value),
                command);
            context.Publish(valueProduced);
        }

        return Task.CompletedTask;
    }

    Task IKernelCommandHandler<SendValue>.HandleAsync(SendValue command, KernelInvocationContext context)
    {
        SetValue(command.Name, command.FormattedValue.Value.Trim('"'));
        return Task.CompletedTask;
    }

    private void SetValue(string valueName, string value)
        => _variables[valueName] = value;

    async Task IKernelCommandHandler<SubmitCode>.HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        var parsedRequests = ParseRequests(command.Code).ToArray();
        var diagnostics = parsedRequests.SelectMany(r => r.Diagnostics).ToArray();

        PublishDiagnostics(context, command, diagnostics);

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            context.Fail(command);
            return;
        }

        try
        {
            foreach (var parsedRequest in parsedRequests)
            {
                await HandleRequestAsync(parsedRequest, command, context);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException || ex is HttpRequestException)
        {
            context.Fail(command, message: ex.Message);
        }
    }

    private async Task HandleRequestAsync(
        ParsedHttpRequest parsedRequest,
        KernelCommand command,
        KernelInvocationContext context)
    {
        var cancellationToken = context.CancellationToken;
        var requestMessage = GetRequestMessage(parsedRequest);

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
            await Task.Delay(_responseDelayThresholdInMilliseconds);

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

    private static HttpRequestMessage GetRequestMessage(ParsedHttpRequest parsedRequest)
    {
        var requestMessage = new HttpRequestMessage(new HttpMethod(parsedRequest.Verb), parsedRequest.Address);
        if (!string.IsNullOrWhiteSpace(parsedRequest.Body))
        {
            requestMessage.Content = new StringContent(parsedRequest.Body);
        }

        foreach (var kvp in parsedRequest.Headers)
        {
            switch (kvp.Key.ToLowerInvariant())
            {
                case "content-type":
                    if (requestMessage.Content is null)
                    {
                        requestMessage.Content = new StringContent("");
                    }
                    requestMessage.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(kvp.Value);
                    break;
                case "accept":
                    requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(kvp.Value));
                    break;
                case "user-agent":
                    requestMessage.Headers.UserAgent.Add(ProductInfoHeaderValue.Parse(kvp.Value));
                    break;
                default:
                    requestMessage.Headers.Add(kvp.Key, kvp.Value);
                    break;
            }
        }

        return requestMessage;
    }

    private async Task<HttpResponse> GetResponseWithTimingAsync(
        HttpRequestMessage requestMessage,
        CancellationToken cancellationToken)
    {
        HttpResponse response;
        var stopWatch = Stopwatch.StartNew();

        try
        {
            var responseMessage = await _client.SendAsync(requestMessage, cancellationToken);
            response = (await responseMessage.ToHttpResponseAsync(cancellationToken))!;
        }
        finally
        {
            stopWatch.Stop();
        }

        response.ElapsedMilliseconds = stopWatch.Elapsed.TotalMilliseconds;
        return response;
    }

    private void PublishDiagnostics(KernelInvocationContext context, KernelCommand command, IEnumerable<Diagnostic> diagnostics)
    {
        if (diagnostics.Any())
        {
            var formattedDiagnostics =
                diagnostics
                    .Select(d => d.ToString())
                    .Select(text => new FormattedValue(PlainTextFormatter.MimeType, text))
                    .ToImmutableArray();

            context.Publish(new DiagnosticsProduced(diagnostics, command, formattedDiagnostics));
        }
    }

    Task IKernelCommandHandler<RequestDiagnostics>.HandleAsync(RequestDiagnostics command, KernelInvocationContext context)
    {
        var requestsAndDiagnostics = InterpolateAndGetDiagnostics(command.Code);
        var diagnostics = requestsAndDiagnostics.SelectMany(r => r.Diagnostics);
        PublishDiagnostics(context, command, diagnostics);
        return Task.CompletedTask;
    }

    private IEnumerable<(string Request, List<Diagnostic> Diagnostics)> InterpolateAndGetDiagnostics(string code)
    {
        var parseResult = HttpRequestParser.Parse(code);

        foreach (var diagnostic in parseResult.GetDiagnostics())
        {
            // FIX: (InterpolateAndGetDiagnostics) 
        }



        var lines = code.Split('\n');

        var result = new List<(string Request, List<Diagnostic>)>();
        var currentLines = new List<string>();
        var currentDiagnostics = new List<Diagnostic>();

        for (var line = 0; line < lines.Length; line++)
        {
            var lineText = lines[line];
            if (IsRequest.IsMatch(lineText))
            {
                if (MightContainRequest(currentLines))
                {
                    var requestCode = string.Join('\n', currentLines);
                    result.Add((requestCode, currentDiagnostics));
                }

                currentLines = new List<string>();
                currentDiagnostics = new List<Diagnostic>();
            }

            var rebuiltLine = new StringBuilder();
            var lastStart = 0;
            var interpolationStart = lineText.IndexOf(InterpolationStartMarker);
            while (interpolationStart >= 0)
            {
                rebuiltLine.Append(lineText[lastStart..interpolationStart]);
                var interpolationEnd = lineText.IndexOf(InterpolationEndMarker, interpolationStart + InterpolationStartMarker.Length);
                if (interpolationEnd < 0)
                {
                    // no end marker
                    // TODO: error?
                }

                var variableName = lineText[(interpolationStart + InterpolationStartMarker.Length)..interpolationEnd];
                if (_variables.TryGetValue(variableName, out var value))
                {
                    rebuiltLine.Append(value);
                }
                else
                {
                    // no variable found; keep old code and report diagnostic
                    rebuiltLine.Append(InterpolationStartMarker);
                    rebuiltLine.Append(variableName);
                    rebuiltLine.Append(InterpolationEndMarker);
                    var position = new LinePositionSpan(
                        new LinePosition(line, interpolationStart + InterpolationStartMarker.Length),
                        new LinePosition(line, interpolationEnd));
                    currentDiagnostics.Add(new Diagnostic(position, DiagnosticSeverity.Error, "HTTP404", $"Cannot resolve symbol '{variableName}'"));
                }

                lastStart = interpolationEnd + InterpolationEndMarker.Length;
                interpolationStart = lineText.IndexOf(InterpolationStartMarker, lastStart);
            }

            rebuiltLine.Append(lineText[lastStart..]);
            currentLines.Add(rebuiltLine.ToString());
        }

        if (MightContainRequest(currentLines))
        {
            var requestCode = string.Join('\n', currentLines);
            result.Add((requestCode, currentDiagnostics));
        }

        return result;
    }

    private static bool MightContainRequest(IEnumerable<string> lines)
    {
        return lines.Any(line => IsRequest.IsMatch(line));
    }

    private IEnumerable<ParsedHttpRequest> ParseRequests(string requests)
    {
        var parseResult = HttpRequestParser.Parse(requests);
        var parsedRequests = new List<ParsedHttpRequest>();

        foreach (var requestNode in parseResult.SyntaxTree.RootNode!.ChildNodes.OfType<HttpRequestNode>())
        {
            var headers =
                requestNode.HeadersNode?.HeaderNodes.Select(h => KeyValuePair.Create(h.NameNode.Text, h.ValueNode.Text)).ToArray()
                ??
                Array.Empty<KeyValuePair<string, string>>();

            var diagnostics = requestNode.GetDiagnostics().ToList();

            var uriResult = requestNode.UrlNode.TryGetUri(BindExpressionValues);
            Uri? address = null;
            if (uriResult.IsSuccessful)
            {
                address = uriResult.Value;
            }
            diagnostics.AddRange(uriResult.Diagnostics);

            var methodNodeText = requestNode.MethodNode?.Text;

            var bodyResult = requestNode.BodyNode?.TryGetBody(BindExpressionValues);
            string body = null;
            if (bodyResult is not null)
            {
                if (bodyResult.IsSuccessful)
                {
                    body = bodyResult.Value;
                }

                diagnostics.AddRange(bodyResult.Diagnostics);
            }

            var parsedRequest = new ParsedHttpRequest(
                methodNodeText,
                address,
                body: body,
                headers: headers,
                diagnostics);

            if (_useNewParser)
            {
                parsedRequests.Add(parsedRequest);
            }

            // FIX: (ParseRequests) 
        }

        if (_useNewParser)
        {
            return parsedRequests;
        }

        foreach (var (request, diagnostics) in InterpolateAndGetDiagnostics(requests))
        {
            var body = new StringBuilder();
            string? verb = null;
            string? address = null;
            var headerValues = new Dictionary<string, string>();
            var lines = request.Split(new[] { '\n' });
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                if (verb is null)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    {
                        continue;
                    }

                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    verb = parts[0].Trim();
                    address = parts[1].Trim();
                }
                else if (!string.IsNullOrWhiteSpace(line) && IsHeader.Matches(line) is { } matches && matches.Count != 0)
                {
                    foreach (Match match in matches)
                    {
                        var key = match.Groups["key"].Value;
                        var value = match.Groups["value"].Value.Trim();
                        headerValues[key] = value;
                    }
                }
                else
                {
                    for (; index < lines.Length; index++)
                    {
                        body.AppendLine(lines[index]);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(verb))
            {
                throw new InvalidOperationException("Cannot perform HttpRequest without a valid verb.");
            }

            var uri = GetAbsoluteUriString(address);
            var bodyText = body.ToString().Trim();
            parsedRequests.Add(new ParsedHttpRequest(verb, uri, bodyText, headerValues.ToList(), diagnostics));
        }

        return parsedRequests;
    }

    private HttpBindingResult<object?> BindExpressionValues(HttpExpressionNode node)
    {
        var variableName = node.Text;
        var expression = variableName;

        if (_variables.TryGetValue(expression, out var value))
        {
            return  HttpBindingResult<object?>.Success(value);
        }

        return HttpBindingResult<object?>.Failure(node.CreateDiagnostic($"Undefined value: {variableName}"));
    }

    private Uri GetAbsoluteUriString(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException("Cannot perform HttpRequest without a valid uri.");
        }

        var uri = new Uri(address, UriKind.RelativeOrAbsolute);

        if (!uri.IsAbsoluteUri)
        {
            throw new InvalidOperationException($"Cannot use relative path {uri} without a base address.");
        }

        return uri;
    }

    private class ParsedHttpRequest
    {
        public ParsedHttpRequest(
            string verb, 
            Uri address, 
            string body, 
            IReadOnlyList<KeyValuePair<string, string>> headers, 
            IReadOnlyList<Diagnostic> diagnostics)
        {
            Verb = verb;
            Address = address;
            Body = body;
            Headers = headers;
            Diagnostics = diagnostics;
        }

        public string Verb { get; }
        public Uri Address { get; }
        public string Body { get; }
        public IReadOnlyList<KeyValuePair<string, string>> Headers { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
    }
}
