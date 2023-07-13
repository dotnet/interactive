// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.DotNet.Interactive.HttpRequest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

internal sealed class HttpDocumentSnapshotParser
{
    private static readonly string[] CommentPrefixes = new[] { "#", "//" };
    private const string RequestSeparator = "###";

    private enum ParseState
    {
        BeforeHttpMethod,
        InRequestHeaders,
        InRequestBody
    }

    private static readonly Regex s_regexUrl = new("""
        ^(?<method>get|post|patch|put|delete|head|options|trace) # HTTP verbs only
        \s+                                                      # Followed by at least one space
        (?<url>.+?(?=\s?HTTP/.*|$))                              # Handle embedded spaces (eg 'https://localhost/id={{$randomInt 3 4}}'
        \s?
        (?<version>HTTP/.*)?
        """, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

    private static readonly Regex s_regexHeader = new("""
        ^(?<name>[\w-]+):   # Header name. No space between name and colon
        \s*(?<value>.*)     # whitespace is allowed after the colon before the value
        """, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

    private static readonly Regex s_regexVariable = new(@"^(?<name>@[^\s]+)\s*(?<equals>=)\s*(?<value>.+)", RegexOptions.Compiled);
    private static readonly Regex s_regexRef = new(@"{{[\w]*}}", RegexOptions.Compiled);

    private readonly ITextSnapshot _textSnapshot;
    private readonly HttpDocumentSnapshot _documentSnapshot;
    private ParseState _parseState;

    public List<ParseItem> Items { get; }
    public List<Request> Requests { get; }
    public Dictionary<string, ParsedVariable> Variables { get; }
    public Dictionary<ParseItem, IReadOnlyList<Error>> AllErrors { get; }

    public HttpDocumentSnapshotParser(HttpDocumentSnapshot documentSnapshot)
    {
        _documentSnapshot = documentSnapshot;
        _textSnapshot = documentSnapshot.TextSnapshot;

        Items = new();
        Requests = new();
        Variables = new();
        AllErrors = new();

        _parseState = ParseState.BeforeHttpMethod;
    }

    public void Parse()
    {
        foreach (ITextSnapshotLine line in _textSnapshot.Lines)
        {
            string lineText = line.GetTextIncludingLineBreak();

            ParseLine(line.Start, lineText);
        }

        OrganizeItems();
        ValidateDocument();
    }

    private void ParseLine(int start, string line)
    {
        string trimmedLine = line.TrimEnd();

        if (_parseState == ParseState.BeforeHttpMethod)
        {
            if (trimmedLine == string.Empty)
            {
                Items.Add(ToParseItem(line, start, ItemType.EmptyLine));
            }
            else if (CommentPrefixes.Any(trimmedLine.StartsWith))
            {
                Items.Add(ToParseItem(line, start, ItemType.Comment, false));
            }
            else if (IsMatch(s_regexVariable, trimmedLine, out Match matchVar))
            {
                ParseItem variableNameItem = ToParseItem(matchVar, start, "name", ItemType.VariableName, false)!;
                ParseItem variableValueItem = ToParseItem(matchVar, start, "value", ItemType.VariableValue, true)!;

                Items.Add(variableNameItem);
                Items.Add(variableValueItem);

                string expandedValue = variableValueItem.ExpandVariables(variableValueItem.Text, Variables);
                ParsedVariable variable = new ParsedVariable(variableNameItem, variableValueItem, expandedValue);

                Variables[variable.VariableName] = variable;
            }
            else if (IsMatch(s_regexUrl, trimmedLine, out Match matchUrl))
            {
                ParseItem method = ToParseItem(matchUrl, start, "method", ItemType.Method)!;
                ParseItem url = ToParseItem(matchUrl, start, "url", ItemType.Url)!;
                ParseItem? version = ToParseItem(matchUrl, start, "version", ItemType.Version);

                Request request = new Request(_documentSnapshot, method, url, version);
                Requests.Add(request);

                Items.Add(request);
                Items.Add(method);
                Items.Add(url);

                if (version != null)
                {
                    Items.Add(version);
                }

                _parseState = ParseState.InRequestHeaders;
            }
        }
        else if (_parseState == ParseState.InRequestHeaders)
        {
            if (CommentPrefixes.Any(trimmedLine.StartsWith))
            {
                Items.Add(ToParseItem(line, start, ItemType.Comment, false));

                if (trimmedLine.StartsWith(RequestSeparator))
                {
                    _parseState = ParseState.BeforeHttpMethod;
                }
            }
            else if (trimmedLine.Length == 0)
            {
                Items.Add(ToParseItem(line, start, ItemType.EmptyLine));
                _parseState = ParseState.InRequestBody;
            }
            else
            {
                bool isMatch = IsMatch(s_regexHeader, trimmedLine, out Match matchHeader);
                if (!isMatch)
                {
                    // Expecting a header but this doesn't look like one so add an error to the item. We only want to
                    // flag the first item I think, otherwise every line after this one will be squiggled. Simplest way
                    // to do that is change the state to InRequestBody and gobble the rest up as part of the body
                    ParseItem errorLine = ToParseItem(line, start, ItemType.Body, false);
                    Items.Add(errorLine);
                    AllErrors[errorLine] = new[] { Errors.NotAValidHttpHeader.WithFormat(trimmedLine) };
                    _parseState = ParseState.InRequestBody;
                }
                else
                {
                    // If the match succeed there should be a name and a value unless the value is missing The code which organizes assumes these are always in
                    // a pair.So we should only add the header if we also have a value
                    ParseItem? headerName = ToParseItem(matchHeader, start, "name", ItemType.HeaderName);
                    ParseItem? headerValue = ToParseItem(matchHeader, start, "value", ItemType.HeaderValue);
                    if (headerName is not null && headerValue is not null)
                    {
                        Items.Add(headerName);
                        Items.Add(headerValue);
                    }
                    else
                    {
                        // Most likely the value hasn't been specified. Treat the line as a body and show an error 
                        ParseItem errorLine = ToParseItem(line, start, ItemType.Body, false);
                        Items.Add(errorLine);
                        AllErrors[errorLine] = new[] { Errors.HttpHeaderMissingValue.WithFormat(trimmedLine) };
                        _parseState = ParseState.InRequestBody;
                    }
                }
            }
        }
        else if (_parseState == ParseState.InRequestBody)
        {
            if (CommentPrefixes.Any(trimmedLine.StartsWith))
            {
                Items.Add(ToParseItem(line, start, ItemType.Comment, false));

                if (trimmedLine.StartsWith(RequestSeparator))
                {
                    _parseState = ParseState.BeforeHttpMethod;
                }
            }
            else
            {
                Items.Add(ToParseItem(line, start, ItemType.Body));
            }
        }
    }

    public static bool IsMatch(Regex regex, string line, out Match match)
    {
        match = regex.Match(line);
        return match.Success;
    }

    private ParseItem ToParseItem(string line, int start, ItemType type, bool supportsVariableReferences = true)
    {
        IReadOnlyList<ParseItem> references = Array.Empty<ParseItem>();
        if (supportsVariableReferences)
        {
            references = GetVariableReferences(line, start);
        }

        ParseItem item = new ParseItem(start, line, _documentSnapshot, type, references);

        return item;
    }

    private ParseItem? ToParseItem(Match match, int start, string groupName, ItemType type, bool supportsVariableReferences = true)
    {
        Group? group = match.Groups[groupName];

        if (string.IsNullOrEmpty(group.Value))
        {
            return null;
        }

        return ToParseItem(group.Value, start + group.Index, type, supportsVariableReferences);
    }

    private IReadOnlyList<ParseItem> GetVariableReferences(string text, int start)
    {
        List<ParseItem>? references = null;

        foreach (Match match in s_regexRef.Matches(text))
        {
            if (ToParseItem(match.Value, start + match.Index, ItemType.Reference, false) is ParseItem reference)
            {
                references ??= new();
                references.Add(reference);
            }
        }

        return references ?? (IReadOnlyList<ParseItem>)Array.Empty<ParseItem>();
    }

    private void ValidateDocument()
    {
        foreach (ParseItem item in Items)
        {
            // Variable references
            foreach (ParseItem? reference in item.References)
            {
                string referenceName = reference.Text.Trim('{', '}');
                if (!Variables.TryGetValue(referenceName, out var variable))
                {
                    AllErrors[reference] = new[] { Errors.VariableNotDefined.WithFormat(referenceName) };
                }
                else
                {
                    if (reference.Start < variable!.Value.End)
                    {
                        AllErrors[reference] = new[] { Errors.VariableReferencedBeforeDefinition.WithFormat(referenceName) };
                    }
                }
            }

            // URLs
            if (item.Type == ItemType.Url)
            {
                string uri = item.ExpandVariables(item.Text, Variables);

                if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
                {
                    AllErrors[item] = new[] { Errors.NotValidAbsoluteURI.WithFormat(uri) };
                }
            }
        }
    }

    private class Errors
    {
        // TODO: Figure out a way to pass localized strings from consumers that link in the parser source.
        // Here's an example of how a source package in the Roslyn repo handles resource strings -
        // http://index/?rightProject=Microsoft.CodeAnalysis.Collections.Package&file=Microsoft.CodeAnalysis.Collections.Package.csproj&line=34
        public static Error VariableNotDefined { get; } = new(nameof(VariableNotDefined), /* Strings.VariableNotDefinedError */ "", ErrorCategory.Warning);
        public static Error VariableReferencedBeforeDefinition { get; } = new(nameof(VariableReferencedBeforeDefinition), /* Strings.VariableReferencedBeforeDefinition */ "", ErrorCategory.Warning);
        public static Error NotValidAbsoluteURI { get; } = new(nameof(NotValidAbsoluteURI), /* Strings.NotValidAbsoluteURIError */ "", ErrorCategory.Warning);
        public static Error NotAValidHttpHeader { get; } = new(nameof(NotAValidHttpHeader), /* Strings.NotAValidHttpHeader */ "", ErrorCategory.Error);
        public static Error HttpHeaderMissingValue { get; } = new(nameof(HttpHeaderMissingValue), /* Strings.HttpHeaderMissingValue */ "", ErrorCategory.Error);
    }

    // NOTE: This method does some evil casting to allow it to mutate what we want to be thought of as
    //        immutable structures. This is the easiest way for now to get a mostly immutable http snapshot
    //        document.
    private void OrganizeItems()
    {
        Request? currentRequest = null;
        ParseItem? previousItem = null;

        for (int itemIndex = 0; itemIndex < Items.Count; itemIndex++)
        {
            ParseItem item = Items[itemIndex];

            if (item.Type == ItemType.Method)
            {
                if (previousItem is Request request)
                {
                    currentRequest = request;
                }
            }
            else if (currentRequest != null)
            {
                if (item.Type == ItemType.HeaderName)
                {
                    if (itemIndex + 1 < Items.Count)
                    {
                        ParseItem nextItem = Items[itemIndex + 1];
                        Header header = new Header(item, nextItem);

                        List<Header> mutableHeaders = (List<Header>)currentRequest.Headers;
                        mutableHeaders.Add(header);

                        List<ParseItem> mutableChildren = (List<ParseItem>)currentRequest.Children;
                        mutableChildren.Add(header.Name);
                        mutableChildren.Add(header.Value);
                    }
                }
                else if (item.Type == ItemType.Body)
                {
                    List<ParseItem> mutableChildren = (List<ParseItem>)currentRequest.Children;

                    if (string.IsNullOrWhiteSpace(item.Text))
                    {
                        // Add so that any whitespace between the request method and the request separator
                        // are valid positions for determine enabling Send Request context menu item
                        if (previousItem?.Type != ItemType.Body)
                        {
                            mutableChildren.Add(item);
                        }
                        continue;
                    }

                    string prevEmptyLine = previousItem?.Type == ItemType.Body && string.IsNullOrWhiteSpace(previousItem.Text) ? previousItem.Text : string.Empty;
                    currentRequest.Body += prevEmptyLine + item.Text;

                    mutableChildren.Add(item);
                }
                else if (item.Type == ItemType.Comment || item.Type == ItemType.EmptyLine)
                {
                    if (item.Text.StartsWith(RequestSeparator))
                    {
                        currentRequest = null;
                    }
                    else
                    {
                        List<ParseItem> mutableChildren = (List<ParseItem>)currentRequest.Children;
                        mutableChildren.Add(item);
                    }
                }
            }

            previousItem = item;
        }
    }
}
