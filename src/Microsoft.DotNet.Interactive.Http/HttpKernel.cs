// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Http.Parsing;
using Microsoft.DotNet.Interactive.ValueSharing;

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
        var variableName = node.Text;
        var expression = variableName;

        if (_variables.TryGetValue(expression, out var value))
        {
            return node.CreateBindingSuccess(value);
        }

        var bindingResult = MatchExpressionValue(node, expression);
        if ( bindingResult is not null)
        {
            return bindingResult;
        } else
        {
            return node.CreateBindingFailure(HttpDiagnostics.CannotResolveSymbol(variableName));
        }

        
    }

    private HttpBindingResult<object?>? MatchExpressionValue(HttpExpressionNode node, string expression)
    {

        var guidPattern = new Regex(@$"\{"$guid"}", RegexOptions.Compiled);
        var dateTimePattern = new Regex(@$"\{"$datetime"}\s(?<type>rfc1123|iso8601|'.+'|"".+"")(?:\s(?<offset>-?\d+)\s(?<option>y|M|Q|w|d|h|m|s|ms))?", RegexOptions.Compiled);
        var localDateTimePattern = new Regex(@$"\{"$localDatetime"}\s(?<type>rfc1123|iso8601|'.+'|"".+"")(?:\s(?<offset>-?\d+)\s(?<option>y|M|Q|w|d|h|m|s|ms))?", RegexOptions.Compiled);
        var randomIntPattern = new Regex(@$"\{"$randomInt"}(?:\s(?<parameters>-?\d+)){{0,2}}", RegexOptions.Compiled);
        var timestampPattern = new Regex(@$"\{"$timestamp"}(?:\s(?<offset>-?\d+)\s(?<option>y|M|Q|w|d|h|m|s|ms))?", RegexOptions.Compiled);

        var guidMatches = guidPattern.Matches(expression);
        var dateTimeMatches = dateTimePattern.Matches(expression);
        var localDateTimeMatches = localDateTimePattern.Matches(expression);
        var randomIntMatches = randomIntPattern.Matches(expression);
        var timestampMatches = timestampPattern.Matches(expression);

        if (guidMatches.Count > 0)
        {
            return node.CreateBindingSuccess(Guid.NewGuid().ToString());
        } else if(dateTimeMatches.Count > 0)
        {
            return GetDateTime(node, expression, dateTimeMatches, dateTimePattern);
        } else if(localDateTimeMatches.Count > 0)
        {
            return GetDateTime(node, expression, localDateTimeMatches, localDateTimePattern);
        } else if(randomIntMatches.Count > 0)
        {
            return GetRandInt(node, expression, randomIntMatches);
        }else if(timestampMatches.Count > 0)
        {
            return GetTimestamp(node, expression, timestampMatches);
        }
        return null;
    }

    private HttpBindingResult<object?> GetTimestamp(HttpExpressionNode node, string expression, MatchCollection matches)
    {
        var text = expression;
        var currentDateTimeOffset = DateTimeOffset.UtcNow;
        
        DateTimeOffset dateTimeOffset = currentDateTimeOffset;
        var match = matches.FirstOrDefault();
        if (match?.Groups.Count == 3)
        {
            try
            {
                if (match.Groups["offset"].Success && match.Groups["option"].Success
                    && int.TryParse(match.Groups["offset"].Value, out int offset)
                    && offset != 0)
                {
                    dateTimeOffset = dateTimeOffset.AddOffset(offset, match.Groups["option"].Value);
                }

                text = string.Concat(text.Substring(0, match.Index), dateTimeOffset.ToUnixTimeSeconds().ToString(), text.Substring(match.Index + match.Value.Length));
            }
            catch (ArgumentException)
            {
                return node.CreateBindingFailure(HttpDiagnostics.InvalidOffset(match.Groups["offset"].Value));
            }
            return node.CreateBindingSuccess(text);
        } else
        {
            return node.CreateBindingFailure(HttpDiagnostics.TimestampFormatError(expression));
        }
        
    }

    private HttpBindingResult<object?> GetDateTime(HttpExpressionNode node, string expression, MatchCollection matches, Regex pattern)
    {
        var text = expression;
        var currentDateTimeOffset = DateTimeOffset.UtcNow;

        // We just pre-matched the prefix, so we need to re-match the full pattern to get the type, offset, and option
        matches = pattern.Matches(text);

        string format;
        DateTimeOffset dateTimeOffset = currentDateTimeOffset;
        var match = matches.FirstOrDefault();
        if (match?.Groups.Count == 4)
        {
            try
            {
                IFormatProvider formatProvider = Thread.CurrentThread.CurrentUICulture;

                Group type = match.Groups["type"];
                if (match.Groups["offset"].Success && match.Groups["option"].Success)
                {
                    if (int.TryParse(match.Groups["offset"].Value, out int offset) && offset != 0)
                    {
                        dateTimeOffset = dateTimeOffset.AddOffset(offset, match.Groups["option"].Value);
                    }
                }

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
                    format = type.Value.Substring(1, type.Value.Length - 2);
                }

                text = dateTimeOffset.ToString(format, formatProvider);

                if(!DateTimeOffset.TryParse(text, out dateTimeOffset) )
                {
                    return node.CreateBindingFailure(HttpDiagnostics.DateTimePatternMatchError(expression));
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return node.CreateBindingFailure(HttpDiagnostics.DateTimePatternMatchError(expression));
            }
            return node.CreateBindingSuccess(text);
        } 
        else
        {
            return node.CreateBindingFailure(HttpDiagnostics.DateTimePatternMatchError(expression));
        } 

        
    }

    private HttpBindingResult<object?> GetRandInt(HttpExpressionNode node, string text, MatchCollection matches)
    {

        Random random = new();
        
        var match = matches.FirstOrDefault();
        if(match != null)
        {
            (int? Min, int? Max)? parameters = ParseParametersFromMatch(match);

            if (parameters is not null)
            {
                int? min = parameters.Value.Min;
                int? max = parameters.Value.Max;

                if (!min.HasValue && !max.HasValue)
                {
                    text = string.Concat(text.Substring(0, match.Index), random.Next().ToString(), text.Substring(match.Index + match.Value.Length));
                }
                else if (!min.HasValue && max.HasValue)
                {
                    text = string.Concat(text.Substring(0, match.Index), random.Next(max.Value).ToString(), text.Substring(match.Index + match.Value.Length));
                }
                else if (min.HasValue && max.HasValue)
                {
                    text = string.Concat(text.Substring(0, match.Index), random.Next(min.Value, max.Value).ToString(), text.Substring(match.Index + match.Value.Length));
                }
            }

            return node.CreateBindingSuccess(text);
        } else
        {
            return node.CreateBindingFailure(HttpDiagnostics.CannotResolveSymbol(text));
        }
        
    }

    internal static (int? min, int? max)? ParseParametersFromMatch(Match match)
    {
        if (match.Success)
        {
            Group group = match.Groups["parameters"];
            if (group.Captures.Count == 0)
            {
                return (null, null);
            }
            else if (group.Captures.Count == 1
                && int.TryParse(group.Captures[0].Value, out int max))
            {
                return (null, max);
            }
            else if (group.Captures.Count == 2
                && int.TryParse(group.Captures[0].Value, out int min)
                && int.TryParse(group.Captures[1].Value, out max)
                && min <= max)
            {
                return (min, max);
            }
        }

        return null;
    }

}