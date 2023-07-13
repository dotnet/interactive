// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class ParseItem
{
    public ParseItem(int start, string text, IHttpDocumentSnapshot document, ItemType type)
        : this(start, text, document, type, Array.Empty<ParseItem>())
    {
    }

    public ParseItem(int start, string text, IHttpDocumentSnapshot document, ItemType type, IReadOnlyList<ParseItem> references)
        : this(start, text, document, type, references, null)
    {
    }

    public ParseItem(int start, string text, IHttpDocumentSnapshot document, ItemType type, IReadOnlyList<ParseItem> references, IValueProvider? valueProvider)
    {
        Start = start;
        Text = text;
        TextExcludingLineBreaks = text.TrimEnd();
        DocumentSnapshot = document;
        Type = type;
        References = references;
        ValueProvider = valueProvider;
    }

    public IHttpDocumentSnapshot DocumentSnapshot { get; }

    public ItemType Type { get; }

    public virtual int Start { get; }

    public virtual string Text { get; }
    public virtual string TextExcludingLineBreaks { get; }

    public virtual int End => Start + Text.Length;
    public virtual int EndExcludingLineBreaks => Start + TextExcludingLineBreaks.Length;

    public virtual int Length => End - Start;
    public virtual int LengthExcludingLineBreaks => EndExcludingLineBreaks - Start;

    public SnapshotSpan Span => new SnapshotSpan(Start, Length);

    public IReadOnlyList<ParseItem> References { get; }
    public IValueProvider? ValueProvider { get; }

    public IReadOnlyList<Error> Errors => DocumentSnapshot.GetErrorsForParseItem(this);

    public bool IsValid => Errors.Count == 0;

    public bool Contains(int position)
    {
        return Start <= position && End >= position;
    }

    public string ExpandVariables()
    {
        return ExpandVariables(Text, DocumentSnapshot.Variables);
    }

    public string ExpandVariables(string text, IReadOnlyDictionary<string, ParsedVariable> variablesExpanded)
    {
        return text;
        return DocumentSnapshot.ValueParser.ParseValues(text, variablesExpanded);
    }

    public override string ToString()
    {
        return $"{Type} {Text} [{Start},{End}]";
    }
}