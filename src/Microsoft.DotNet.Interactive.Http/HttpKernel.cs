// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.ValueSharing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Http;

using Diagnostic = CodeAnalysis.Diagnostic;

public class HttpKernel :
    Kernel,
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
    {
        await SetValueAsync(command, context, SetValueAsync);
    }

    private Task SetValueAsync(string valueName, object value, Type? declaredType = null)
    {
        _variables[valueName] = value;
        return Task.CompletedTask;
    }

    async Task IKernelCommandHandler<SubmitCode>.HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        var parseResult = HttpRequestParser.Parse(command.Code);

        var httpRequestResults = parseResult.SyntaxTree.RootNode.ChildNodes.OfType<HttpRequestNode>().Select(n => n.TryGetHttpRequestMessage(BindExpressionValues)).ToArray();

        var diagnostics = httpRequestResults.SelectMany(r => r.Diagnostics).ToArray();

        PublishDiagnostics(context, command, diagnostics);

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            var message = string.Join(Environment.NewLine, diagnostics.Select(d => d.ToString()));
            context.Fail(command, message: message);
            return;
        }

        var requestMessages = httpRequestResults
                              .Where(r => r is { IsSuccessful: true, Value: not null })
                              .Select(r => r.Value!).ToArray();

        try
        {
            foreach (var requestMessage in requestMessages)
            {
                await SendRequestAsync(requestMessage, command, context);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException)
        {
            context.Fail(command, message: ex.Message);
        }
    }

    private async Task SendRequestAsync(HttpRequestMessage requestMessage, KernelCommand command, KernelInvocationContext context)
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

        try
        {
            // null out the current activity so the new one that we create won't be parented to it.
            Activity.Current = null;

            Activity.Current = new Activity("").Start();

            var responseMessage = await _client.SendAsync(requestMessage, cancellationToken);
            response = (await responseMessage.ToHttpResponseAsync(cancellationToken))!;
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
        if (diagnostics.Any())
        {
            var formattedDiagnostics =
                diagnostics
                    .Select(d => d.ToString())
                    .Select(text => new FormattedValue(PlainTextFormatter.MimeType, text))
                    .ToArray();

            context.Publish(
                new DiagnosticsProduced(
                    diagnostics.Select(ToSerializedDiagnostic),
                    command,
                    formattedDiagnostics));
        }

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

        if (_variables.TryGetValue(expression, out var value))
        {
            return node.CreateBindingSuccess(value);
        }
        else
        {
            return MatchExpressionValue(node, expression);
        }


    }

    private HttpBindingResult<object?> MatchExpressionValue(HttpExpressionNode node, string expression)
    {
        const string DateTime = "$datetime";
        const string LocalDateTime = "$localDatetime";
        const string OffsetRegex = """(?:\s+(?<offset>[-+]?[^\s]+)\s+(?<option>[^\s]+))?""";
        const string TypeRegex = """(?:\s+(?<type>rfc1123|iso8601|'.+'|".+"))?""";

        var guidPattern = new Regex(@$"^\$guid$", RegexOptions.Compiled);
        var dateTimePattern = new Regex(@$"^\{DateTime}{TypeRegex}{OffsetRegex}$", RegexOptions.Compiled);
        var localDateTimePattern = new Regex(@$"^\{LocalDateTime}{TypeRegex}{OffsetRegex}$", RegexOptions.Compiled);
        var randomIntPattern = new Regex(@$"^\$randomInt(?:\s+(?<arguments>-?[^\s]+)){{0,2}}$", RegexOptions.Compiled);
        var timestampPattern = new Regex(@$"^\$timestamp{OffsetRegex}$", RegexOptions.Compiled);


        var guidMatches = guidPattern.Matches(expression);
        if (guidMatches.Count == 1)
        {
            return node.CreateBindingSuccess(Guid.NewGuid().ToString());
        }

        if (expression.Contains(DateTime))
        {
            var dateTimeMatches = dateTimePattern.Matches(expression);
            if (dateTimeMatches.Count == 1)
            {
                return GetDateTime(node, DateTime, expression, dateTimeMatches.Single());
            }

            return node.CreateBindingFailure(HttpDiagnostics.IncorrectDateTimeFormat(expression, DateTime));
        }

        if (expression.Contains(LocalDateTime))
        {
            var localDateTimeMatches = localDateTimePattern.Matches(expression);
            if (localDateTimeMatches.Count == 1)
            {
                return GetDateTime(node, LocalDateTime, expression, localDateTimeMatches.Single());
            }

            return node.CreateBindingFailure(HttpDiagnostics.IncorrectDateTimeFormat(expression, LocalDateTime));
        }

        if (expression.Contains("$timestamp"))
        {
            var timestampMatches = timestampPattern.Matches(expression);
            if (timestampMatches.Count == 1)
            {
                return GetTimestamp(node, expression, timestampMatches.Single());
            }

            return node.CreateBindingFailure(HttpDiagnostics.IncorrectTimestampFormat(expression));
        }

        if (expression.Contains("$randomInt"))
        {
            var randomIntMatches = randomIntPattern.Matches(expression);
            if (randomIntMatches.Count == 1)
            {
                return GetRandInt(node, expression, randomIntMatches.Single());
            }

            return node.CreateBindingFailure(HttpDiagnostics.IncorrectRandomIntFormat(expression));
        }

        return node.CreateBindingFailure(HttpDiagnostics.UnableToEvaluateExpression(expression));
    }


    private HttpBindingResult<object?> GetTimestamp(HttpExpressionNode node, string expressionText, Match match)
    {
        if (match.Groups.Count == 3)
        {
            var currentDateTimeOffset = DateTimeOffset.UtcNow;

            if (string.Equals(expressionText, "$timestamp"))
            {
                return node.CreateBindingSuccess(currentDateTimeOffset.ToUnixTimeSeconds().ToString());
            }

            if (match.Groups["offset"].Success && match.Groups["option"].Success)
            {

                var offsetString = match.Groups["offset"].Value;
                if (int.TryParse(offsetString, out int offset))
                {
                    var optionString = match.Groups["option"].Value;

                    if (currentDateTimeOffset.TryAddOffset(offset, optionString, out var newDateTimeOffset))
                    {
                        expressionText = newDateTimeOffset.Value.ToUnixTimeSeconds().ToString();
                        return node.CreateBindingSuccess(expressionText);
                    }
                    else
                    {
                        return node.CreateBindingFailure(HttpDiagnostics.InvalidOption(expressionText, optionString));
                    }
                }
                else
                {
                    return node.CreateBindingFailure(HttpDiagnostics.InvalidOffset(expressionText, offsetString));
                }
            }

        }
        return node.CreateBindingFailure(HttpDiagnostics.IncorrectTimestampFormat(expressionText));

    }

    private HttpBindingResult<object?> GetDateTime(HttpExpressionNode node, string dateTimeType, string expressionText, Match match)
    {
        if (match.Groups.Count == 4)
        {
            var currentDateTimeOffset = DateTimeOffset.UtcNow;
            if (match.Groups["offset"].Success && match.Groups["option"].Success)
            {
                var offsetString = match.Groups["offset"].Value;
                if (int.TryParse(offsetString, out int offset))
                {
                    var optionString = match.Groups["option"].Value;
                    if (currentDateTimeOffset.TryAddOffset(offset, optionString, out var newDateTimeOffset))
                    {
                        currentDateTimeOffset = newDateTimeOffset.Value;
                    }
                    else
                    {
                        return node.CreateBindingFailure(HttpDiagnostics.InvalidOption(expressionText, optionString));
                    }
                }
                else
                {
                    return node.CreateBindingFailure(HttpDiagnostics.InvalidOffset(expressionText, offsetString));
                }
            }
            string format;
            var formatProvider = Thread.CurrentThread.CurrentUICulture;
            var type = match.Groups["type"];

            string text;
            if (string.IsNullOrWhiteSpace(type.Value))
            {
                text = currentDateTimeOffset.ToString();
            }
            else
            {
                if (string.Equals(type.Value, "rfc1123", StringComparison.OrdinalIgnoreCase))
                {
                    // For RFC1123, we want to be sure to use the invariant culture,
                    // since we are potentially overriding the format for local date time
                    // we should explicitly set the format provider to invariant culture
                    formatProvider = CultureInfo.InvariantCulture;
                    format = "r";
                }
                else if (string.Equals(type.Value, "iso8601", StringComparison.OrdinalIgnoreCase))
                {
                    format = "o";
                }
                else
                {
                    // This substring exists to strip out the double quotes that are expected in a custom format
                    format = type.Value.Substring(1, type.Value.Length - 2);
                }

                text = currentDateTimeOffset.ToString(format, formatProvider);
            }

            if (DateTimeOffset.TryParse(text, out _))
            {
                return node.CreateBindingSuccess(text);
            }
        }
        return node.CreateBindingFailure(HttpDiagnostics.IncorrectDateTimeFormat(expressionText, dateTimeType));
    }

    private HttpBindingResult<object?> GetRandInt(HttpExpressionNode node, string text, Match match)
    {
        if (TryParseArgumentsFromMatch(text, match, out var min, out var max, out var diagnostic))
        {
            Random random = new();
            if (!min.HasValue && !max.HasValue)
            {
                text = random.Next().ToString();
            }
            else if (!min.HasValue && max.HasValue)
            {
                text = random.Next(max.Value).ToString();
            }
            else if (min.HasValue && max.HasValue)
            {
                text = random.Next(min.Value, max.Value).ToString();
            }

            return node.CreateBindingSuccess(text);
        }
        else
        {
            return node.CreateBindingFailure(diagnostic);
        }

        bool TryParseArgumentsFromMatch(string expression, Match match, out int? min, out int? max, [NotNullWhen(false)] out HttpDiagnosticInfo? diagnostic)
        {
            if (match.Success)
            {
                var group = match.Groups["arguments"];
                if (group.Captures.Count == 0)
                {
                    max = null;
                    min = null;
                    diagnostic = null;
                    return true;
                }
                else if (group.Captures.Count == 1)
                {
                    min = null;
                    string maxValueString = group.Captures[0].Value;
                    return TryParseInteger(maxValueString, expression, out max, out diagnostic);

                }

                else if (group.Captures.Count == 2)
                {
                    string minValueString = group.Captures[0].Value;

                    if (!TryParseInteger(minValueString, expression, out min, out diagnostic))
                    {
                        max = null;
                        return false;
                    }

                    string maxValueString = group.Captures[1].Value;

                    if (!TryParseInteger(maxValueString, expression, out max, out diagnostic))
                    {
                        min = null;
                        return false;
                    }

                    if (min > max)
                    {
                        diagnostic = HttpDiagnostics.RandomIntMinMustNotBeGreaterThanMax(expression, min.Value.ToString(), max.Value.ToString());
                        min = null;
                        max = null;
                        return false;
                    }

                    return true;
                }

            }

            min = null;
            max = null;
            diagnostic = HttpDiagnostics.IncorrectRandomIntFormat(expression);
            return false;

            bool TryParseInteger(string valueString, string expression, [NotNullWhen(true)] out int? value, [NotNullWhen(false)] out HttpDiagnosticInfo? diagnostic)
            {
                if (int.TryParse(valueString, out var result))
                {
                    value = result;
                    diagnostic = null;
                    return true;
                }
                else
                {
                    value = null;
                    diagnostic = HttpDiagnostics.InvalidRandomIntArgument(expression, valueString);
                    return false;
                }
            }
        }
    }



}