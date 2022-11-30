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
       IKernelCommandHandler<SendValue>,
       IKernelCommandHandler<SubmitCode>,
       IKernelCommandHandler<RequestDiagnostics>
{
    private readonly HttpClient _client;
    private readonly Argument<Uri> _hostArgument = new();

    private readonly Dictionary<string, string> _variables = new(StringComparer.InvariantCultureIgnoreCase);
    private static readonly Regex PlaceHolderPattern;
    private static readonly Regex LineStartPattern;
    private static readonly Regex IsHeader;

    static HttpRequestKernel()
    {
        PlaceHolderPattern = new Regex(@"(\{\{(?<symbol>[^{}]+)\}\})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        var verbs = string.Join("|",
            typeof(HttpMethod).GetProperties(BindingFlags.Static | BindingFlags.Public).Select(p => p.GetValue(null).ToString()));

        LineStartPattern = new Regex(@"^\s*(" + verbs + ")", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

        IsHeader = new Regex(@"^\s*(?<key>[\w-]+):\s*(?<value>.*)", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public HttpRequestKernel(string name = null, HttpClient client = null)
        : base(name ?? "httpRequest")
    {

        KernelInfo.LanguageName = "http";
        KernelInfo.DisplayName = "Http Request";

        _client = client ?? new HttpClient();
        var setHost = new Command("#!set-host");
        setHost.AddArgument(_hostArgument);
        setHost.SetHandler(context =>
        {
            BaseAddress = context.ParseResult.GetValueForArgument(_hostArgument);
        });
        AddDirective(setHost);

        RegisterForDisposal(_client);
    }

    public Uri BaseAddress
    {
        get;
        set;
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
        var requests = ParseRequests(command.Code);
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
            var formattedValue = new FormattedValue(PlainTextFormatter.MimeType, response.ToDisplayString(PlainTextFormatter.MimeType));
            context.Publish(new ReturnValueProduced(response, command, new[] { formattedValue }));
        }
    }


    public Task HandleAsync(RequestDiagnostics command, KernelInvocationContext context)
    {
        foreach (var (request, start, length) in GetAllRequests(command.Code))
        {
            var diagnostics = GetInterpolationDiagnostics(request, start, length, _variables, command.Code);
            var formattedDiagnostics =
                diagnostics
                    .Select(d => d.ToString())
                    .Select(text => new FormattedValue(PlainTextFormatter.MimeType, text))
                    .ToImmutableArray();
            context.Publish(new DiagnosticsProduced(diagnostics, command, formattedDiagnostics));
        }
        return Task.CompletedTask;
    }

    private IEnumerable<Diagnostic> GetInterpolationDiagnostics(string request, int start, int length,
        IReadOnlyDictionary<string, string> variables, string originalCode)
    {
        var diagnostics = new List<Diagnostic>();
        var matches = PlaceHolderPattern.Matches(request);
        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var symbol = match.Groups["symbol"]?.Value.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(symbol))
            {
                var toReplace = $"{{{{{symbol}}}}}";

                switch (symbol)
                {
                    case "host" when BaseAddress is { }:
                        break;
                    default:
                        if (!variables.TryGetValue(symbol, out _))
                        {
                            var character = 0;
                            var line = 1;
                            for (var x = 0; x < match.Index; x++)
                            {
                                if (originalCode[x] == '\n')
                                {
                                    line++;
                                    character = 0;
                                }
                                else
                                {
                                    character++;
                                }
                            }
                            var position = new LinePositionSpan(
                                new LinePosition(line, character), 
                                new LinePosition(line, character + toReplace.Length - 1));
                            diagnostics.Add(new Diagnostic(position, DiagnosticSeverity.Error, originalCode, $"Cannot resolve symbol {toReplace}"));
                        }

                        break;
                }

            }
        }

        return diagnostics;
    }

    public IEnumerable<(string request, int start, int length)> GetAllRequests(string requests)
    {
        var splitRequests = new List<(string request, int start, int length)>();
        requests = requests.Replace("\r\n", "\n");
        var matches = LineStartPattern.Matches(requests);
        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];

            var start = match.Index;
            var end = requests.Length;
            var next = i + 1;
            if (next < matches.Count)
            {
                end = matches[next].Index;
            }

            var request = requests.Substring(start, end - start);
            splitRequests.Add(new(request, start, end - start));
        }

        return splitRequests;
    }
    private IEnumerable<HttpRequest> ParseRequests(string requests)
    {
        var parsedRequests = new List<HttpRequest>();

        /*
         * A request as first verb and endpoint (optional version), this command could be multiline
         * optional headers
         * optional body
         */

        foreach (var (request, _, _) in GetAllRequests(requests))
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
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var parts = line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    verb = parts[0].Trim();
                    address = Interpolate(parts[1].Trim(), _variables);
                }
                else if (!string.IsNullOrWhiteSpace(line) && IsHeader.Matches(line) is { } matches)
                {
                    foreach (Match match in matches)
                    {
                        var key = match.Groups["key"].Value;
                        var value = Interpolate(match.Groups["value"].Value.Trim(), _variables);
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

            var bodyText = body.ToString();
            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                bodyText = Interpolate(bodyText, _variables).Trim();
            }

            if (string.IsNullOrWhiteSpace(address) && BaseAddress is null)
            {
                throw new InvalidOperationException("Cannot perform HttpRequest without an valid uri.");
            }

            var uri = new Uri(address, UriKind.RelativeOrAbsolute);

            if (!uri.IsAbsoluteUri && BaseAddress is null)
            {
                throw new InvalidOperationException($"Cannot use relative path {uri} without a base address.");
            }

            uri = uri.IsAbsoluteUri ? uri : new Uri(BaseAddress, uri);


            parsedRequests.Add(new HttpRequest(verb, uri.AbsoluteUri, bodyText, headerValues));

        }

        return parsedRequests;
    }

    private string Interpolate(string template, IReadOnlyDictionary<string, string> variables)
    {
        var result = template;
        var matches = PlaceHolderPattern.Matches(template);
        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var symbol = match.Groups["symbol"]?.Value.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(symbol))
            {
                var toReplace = $"{{{{{symbol}}}}}";

                switch (symbol)
                {
                    case "host" when BaseAddress is { }:
                        result = result.Replace(toReplace, BaseAddress.Host);
                        break;
                    default:
                        if (variables.TryGetValue(symbol, out var symbolValue))
                        {

                        }
                        else
                        {
                            throw new InvalidOperationException($"Cannot resolve replacement for {toReplace}");
                        }
                        result = result.Replace(toReplace, variables[symbol]);
                        break;
                }

            }
        }

        return result;
    }


    public class HttpRequest
    {
        public HttpRequest(string verb, string address, string body, IEnumerable<KeyValuePair<string, string>> headers)
        {
            Verb = verb;
            Address = address;
            Body = body;
            Headers = headers;
        }

        public string Verb { get; }
        public string Address { get; }
        public string Body { get; }
        public IEnumerable<KeyValuePair<string, string>> Headers { get; }
    }

}

