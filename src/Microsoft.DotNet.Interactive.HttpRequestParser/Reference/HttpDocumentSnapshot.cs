// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.HttpRequest.Reference;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

internal partial class HttpDocumentSnapshot : IHttpDocumentSnapshot
{
    private HttpDocumentSnapshot(ITextSnapshot snapshot, IValueParser valueParser)
    {
        TextSnapshot = snapshot;

        HttpDocumentSnapshotParser parser = new HttpDocumentSnapshotParser(this);

        parser.Parse();

        Errors = parser.AllErrors;
        Items = parser.Items;
        Requests = parser.Requests;
        Variables = parser.Variables;
        ValueParser = valueParser;
    }

    public ITextSnapshot TextSnapshot { get; }

    public IValueParser ValueParser { get; }
    public IReadOnlyDictionary<ParseItem, IReadOnlyList<Error>> Errors { get; }
    public IReadOnlyList<ParseItem> Items { get; }

    public IReadOnlyList<Request> Requests { get; }

    public IReadOnlyDictionary<string, ParsedVariable> Variables { get; }

    public static HttpDocumentSnapshot Parse(string code)
    {
        return Parse(new HttpTextSnapshot(code), new NullValueParser(), CancellationToken.None);
    }

    internal class NullValueParser : IValueParser
    {
        public string ParseValues(string text, IReadOnlyDictionary<string, ParsedVariable> variablesExpanded)
        {
            return text;
        }

        public bool TryGetValueProvider<T>(out T provider) where T : class, IValueProvider
        {
            provider = default;
            return false;
        }

        public bool TryGetValueProviderWithMatchingPrefix<T>(string prefix, out T provider) where T : class, IValueProvider
        {
            provider = default;
            return false;
        }

        public ITextSnapshot Snapshot { get; set; }

        public IEnumerable<IValueProvider> ValueProviders { get; }
    }

    private class HttpTextSnapshot : ITextSnapshot
    {
        public HttpTextSnapshot(string code)
        {
            Lines = code.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None).Select((line, lineNumber) => new HttpTextSnapshotLine(line, lineNumber));
        }

        public IEnumerable<ITextSnapshotLine> Lines { get; }
    }


    internal class HttpTextSnapshotLine : ITextSnapshotLine
    {
        public string Text { get; }

        public HttpTextSnapshotLine(string text, int lineNumber)
        {
            Text = text;
            Start = lineNumber;
        }

        public int Start { get; }

        public string GetTextIncludingLineBreak()
        {
            return Text + Environment.NewLine;
        }
    }


    public static HttpDocumentSnapshot Parse(ITextSnapshot snapshot, IValueParser valueParser, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        HttpDocumentSnapshot documentSnapshot = new(snapshot, valueParser);

        return documentSnapshot;
    }

    public ParseItem FindItemFromPosition(int position)
    {
        ParseItem item = Items.LastOrDefault(t => t.Contains(position));
        ParseItem reference = item?.References.FirstOrDefault(v => v.Contains(position));

        // Return the reference if it exist; otherwise the item
        return reference ?? item;
    }

    private static int GetIndexOf<T>(IReadOnlyList<T> items, Func<T, bool> predicate) where T : class
    {
        int i = 0;

        foreach (T item in items)
        {
            if (predicate(item))
            {
                return i;
            }

            ++i;
        }

        return -1;
    }

    public IEnumerable<ParseItem> FindItemsFromSpan(SnapshotSpan span)
    {
        int startItemIndex = GetIndexOf(Items, t => t.Contains(span.Start));

        if (startItemIndex < 0)
        {
            yield break;
        }

        for (int curItemIndex = startItemIndex; curItemIndex < Items.Count; curItemIndex++)
        {
            ParseItem item = Items[curItemIndex];
            if (item.Span.Start > span.End)
            {
                // this item starts after the end of the span. No need to continue looking.
                break;
            }
            else if (item.Span.End >= span.Start)
            {
                yield return item;

                foreach (ParseItem referenceItem in item.References)
                {
                    // Don't break out early as the first entry may have items at the end overlap
                    //   IntersectsWith is used as OverlapsWith doesn't allow empty spans
                    if (referenceItem.Span.IntersectsWith(span))
                    {
                        yield return referenceItem;
                    }
                }
            }
        }
    }

    public IReadOnlyList<Error> GetErrorsForParseItem(ParseItem item)
    {
        if (Errors.TryGetValue(item, out IReadOnlyList<Error> errors))
        {
            return errors!;
        }

        return Array.Empty<Error>();
    }
}
