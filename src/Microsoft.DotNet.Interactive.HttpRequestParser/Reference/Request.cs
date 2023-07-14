// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.HttpRequest.Reference;

using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO: Rationalize this with HttpRequest and ParsedHttpRequest types present in HttpRequestKernel.
internal sealed class Request : ParseItem
{
    private readonly HttpDocumentSnapshot _document;

    public Request(HttpDocumentSnapshot document, ParseItem method, ParseItem url, ParseItem version)
        : base(method.Start, method.Text, document, ItemType.Request)
    {
        _document = document;
        Method = method;
        Url = url;
        Version = version;

        Headers = new List<Header>();

        List<ParseItem> children = new List<ParseItem>() { Method, Url };

        if (Version != null)
        {
            children.Add(Version);
        }

        Children = children;
    }

    public IReadOnlyList<ParseItem> Children { get; }

    public bool RequestHasDocumentErrors => Children.FirstOrDefault(item => item.Errors.Count > 0) != null;

    public ParseItem Method { get; }
    public ParseItem Url { get; }

    public ParseItem Version { get; }

    public IReadOnlyList<Header> Headers { get; }

    public string Body { get; set; }
    public override int Start => Method?.Start ?? 0;
    public override int End => Children.Count > 0 ? Children[Children.Count - 1].End : 0;

    public override string ToString()
    {
        StringBuilder sb = new();

        sb.AppendLine($"{Method.Text} {Url.ExpandVariables()}");

        foreach (Header header in Headers)
        {
            sb.AppendLine($"{header.Name.ExpandVariables()}: {header.Value.ExpandVariables()}");
        }

        if (!string.IsNullOrEmpty(Body))
        {
            sb.AppendLine(ExpandBodyVariables());
        }

        return sb.ToString().Trim();
    }

    // This hash code is used as lookup for finding the state associated with this request. The request objects are re-built
    // every parse so the state needs to be stored externally.
    private int _requestHashCode;
    public int GetRequestHashCode()
    {
        if (_requestHashCode == 0)
        {
            StringBuilder sb = new();

            sb.AppendLine($"{Method.Text} {Url.Text}");

            foreach (Header header in Headers)
            {
                sb.AppendLine($"{header.Name}: {header.Value}");
            }

            if (!string.IsNullOrEmpty(Body))
            {
                sb.AppendLine(Body);
            }
            _requestHashCode = sb.ToString().Trim().GetHashCode();
        }

        return _requestHashCode;
    }

    public string ExpandBodyVariables()
    {
        if (Body == null)
        {
            return string.Empty;
        }

        // Then replace the references with the expanded values
        string clean = Body;

        foreach (var kvp in _document.Variables)
        {
            clean = clean.Replace("{{" + kvp.Key + "}}", kvp.Value.ExpandedValue.Trim());
        }

        return clean.Trim();
    }
}
