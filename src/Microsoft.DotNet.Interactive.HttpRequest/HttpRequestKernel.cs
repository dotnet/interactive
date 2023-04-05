// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
    private readonly HttpClient _client;
    private readonly Argument<Uri> _hostArgument = new();

    private readonly Dictionary<string, string> _variables = new(StringComparer.InvariantCultureIgnoreCase);
    private Uri _baseAddress;
    private static readonly Regex IsRequest;
    private static readonly Regex IsHeader;

    private const string InterpolationStartMarker = "{{";
    private const string InterpolationEndMarker = "}}";

    static HttpRequestKernel()
    {
        var verbs = string.Join("|",
            typeof(HttpMethod).GetProperties(BindingFlags.Static | BindingFlags.Public).Select(p => p.GetValue(null).ToString()));

        IsRequest = new Regex(@"^\s*(" + verbs + ")", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

        IsHeader = new Regex(@"^\s*(?<key>[\w-]+):\s*(?<value>.*)", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public HttpRequestKernel(string name = null, HttpClient client = null)
        : base(name ?? "http")
    {
        KernelInfo.LanguageName = "HTTP";
        KernelInfo.DisplayName = $"{KernelInfo.LocalName} - HTTP Request";

        _client = client ?? new HttpClient();
        var setHost = new Command("#!set-host", "Sets the host name to be used when sending requests using relative URLs");
        setHost.AddArgument(_hostArgument);
        setHost.SetHandler(context =>
        {
            var host = context.ParseResult.GetValueForArgument(_hostArgument);
            BaseAddress = host;
        });
        AddDirective(setHost);

        RegisterForDisposal(_client);
    }

    public Uri BaseAddress
    {
        get => _baseAddress;
        set
        {
            _baseAddress = value;
            SetValue("host", value?.Host);
        }
    }

    public Task HandleAsync(RequestValue command, KernelInvocationContext context)
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

    public Task HandleAsync(SendValue command, KernelInvocationContext context)
    {
        SetValue(command.Name, command.FormattedValue.Value);
        return Task.CompletedTask;
    }

    public void SetValue(string valueName, string value)
    {
        _variables[valueName] = value;
    }

    public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        var requests = ParseRequests(command.Code).ToArray();
        var diagnostics = requests.SelectMany(r => r.Diagnostics).ToArray();

        PublishDiagnostics(context, command, diagnostics);

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            context.Fail(command);
            return;
        }

        foreach (var httpRequest in requests)
        {
            var message = new HttpRequestMessage(new HttpMethod(httpRequest.Verb), httpRequest.Address);
            if (!string.IsNullOrWhiteSpace(httpRequest.Body))
            {
                message.Content = new StringContent(httpRequest.Body);
            }

            foreach (var kvp in httpRequest.Headers)
            {
                switch (kvp.Key.ToLowerInvariant())
                {
                    case "content-type":
                        if (message.Content is null)
                        {
                            message.Content = new StringContent(httpRequest.Body);
                        }
                        message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(kvp.Value);
                        break;
                    case "accept":
                        message.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(kvp.Value));
                        break;
                    case "user-agent":
                        message.Headers.UserAgent.Add(ProductInfoHeaderValue.Parse(kvp.Value));
                        break;
                    default:
                        message.Headers.Add(kvp.Key, kvp.Value);
                        break;
                }
            }

            var response = await _client.SendAsync(message);
            context.Display(response, HtmlFormatter.MimeType, PlainTextFormatter.MimeType);
        }
    }

    private void PublishDiagnostics(KernelInvocationContext context, KernelCommand command, IEnumerable<Diagnostic> diagnostics)
    {
        var formattedDiagnostics =
            diagnostics
                .Select(d => d.ToString())
                .Select(text => new FormattedValue(PlainTextFormatter.MimeType, text))
                .ToImmutableArray();
        context.Publish(new DiagnosticsProduced(diagnostics, command, formattedDiagnostics));
    }

    public Task HandleAsync(RequestDiagnostics command, KernelInvocationContext context)
    {
        var requestsAndDiagnostics = InterpolateAndGetDiagnostics(command.Code);
        var diagnostics = requestsAndDiagnostics.SelectMany(r => r.Diagnostics);
        PublishDiagnostics(context, command, diagnostics);
        return Task.CompletedTask;
    }

    private IEnumerable<(string Request, List<Diagnostic> Diagnostics)> InterpolateAndGetDiagnostics(string code)
    {
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
        //return lines.Any() && lines.Any(line => !string.IsNullOrWhiteSpace(line));
    }
    
    private IEnumerable<HttpRequest> ParseRequests(string requests)
    {
        var parsedRequests = new List<HttpRequest>();

        /*
         * A request as first verb and endpoint (optional version), this command could be multiline
         * optional headers
         * optional body
         */

        foreach (var (request, diagnostics) in InterpolateAndGetDiagnostics(requests))
        {
            var body = new StringBuilder();
            string verb = null;
            string address = null;
            var headerValues = new Dictionary<string, string>();
            var lines = request.Split(new[] {'\n'});
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                if (verb == null)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    {
                        continue;
                    }

                    var parts = line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
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

            var bodyText = body.ToString().Trim();

            if (string.IsNullOrWhiteSpace(address) && BaseAddress is null)
            {
                throw new InvalidOperationException("Cannot perform HttpRequest without a valid uri.");
            }

            var uri = new Uri(address, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri && BaseAddress is null)
            {
                throw new InvalidOperationException($"Cannot use relative path {uri} without a base address.");
            }

            uri = uri.IsAbsoluteUri ? uri : new Uri(BaseAddress, uri);
            parsedRequests.Add(new HttpRequest(verb, uri.AbsoluteUri, bodyText, headerValues, diagnostics));
        }

        return parsedRequests;
    }

    private class HttpRequest
    {
        public HttpRequest(string verb, string address, string body, IEnumerable<KeyValuePair<string, string>> headers, IEnumerable<Diagnostic> diagnostics)
        {
            Verb = verb;
            Address = address;
            Body = body;
            Headers = headers;
            Diagnostics = diagnostics;
        }

        public string Verb { get; }
        public string Address { get; }
        public string Body { get; }
        public IEnumerable<KeyValuePair<string, string>> Headers { get; }
        public IEnumerable<Diagnostic> Diagnostics { get; }
    }
}
