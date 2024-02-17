// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

/// <summary>
/// To aid with testing, we define a special type of text file that can encode additional
/// information in it.  This prevents a test writer from having to carry around multiple sources
/// of information that must be reconstituted.  For example, instead of having to keep around the
/// contents of a file *and* and the location of the cursor, the tester can just provide a
/// string with the "$" character in it.  This allows for easy creation of "FIT" tests where all
/// that needs to be provided are strings that encode every bit of state necessary in the string
/// itself.
/// 
/// The current set of encoded features we support are: 
/// 
/// $$ - The position in the file.  There can be at most one of these.
/// 
/// [| ... |] - A span of text in the file.  There can be many of these and they can be nested
/// and/or overlap the $ position.
/// 
/// {|Name: ... |} A span of text in the file annotated with an identifier.  There can be many of
/// these, including ones with the same name.
/// 
/// Additional encoded features can be added on a case by case basis.
/// </summary>
public static class MarkupTestFile
{
    private const string PositionString = "$$";
    private const string SpanStartString = "[|";
    private const string SpanEndString = "|]";
    private const string NamedSpanStartString = "{|";
    private const string NamedSpanEndString = "|}";

    private static readonly Regex s_namedSpanStartRegex = new Regex(@"\{\| ([-_.A-Za-z0-9\+]+) \:",
        RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

    private static void Parse(
        string input,
        out string output,
        out int? position,
        out Dictionary<string, List<TextSpan>> spans)
    {
        position = null;
        var tempSpans = new Dictionary<string, List<TextSpan>>();

        var outputBuilder = new StringBuilder();

        var currentIndexInInput = 0;
        var inputOutputOffset = 0;

        // A stack of span starts along with their associated annotation name.  [||] spans simply
        // have empty string for their annotation name.
        var spanStartStack = new Stack<Tuple<int, string>>();

        while (true)
        {
            var matches = new List<Tuple<int, string>>();
            AddMatch(input, PositionString, currentIndexInInput, matches);
            AddMatch(input, SpanStartString, currentIndexInInput, matches);
            AddMatch(input, SpanEndString, currentIndexInInput, matches);
            AddMatch(input, NamedSpanEndString, currentIndexInInput, matches);

            var namedSpanStartMatch = s_namedSpanStartRegex.Match(input, currentIndexInInput);
            if (namedSpanStartMatch.Success)
            {
                matches.Add(Tuple.Create(namedSpanStartMatch.Index, namedSpanStartMatch.Value));
            }

            if (matches.Count == 0)
            {
                // No more markup to process.
                break;
            }

            var orderedMatches = matches.OrderBy(t => t, MatchComparer.Instance).ToList();
            if (orderedMatches.Count >= 2 &&
                spanStartStack.Count > 0 &&
                matches[0].Item1 == matches[1].Item1 - 1)
            {
                // We have a slight ambiguity with cases like these:
                //
                // [|]    [|}
                //
                // Is it starting a new match, or ending an existing match.  As a workaround, we
                // special case these and consider it ending a match if we have something on the
                // stack already.
                if ((matches[0].Item2 == SpanStartString && matches[1].Item2 == SpanEndString && spanStartStack.Peek().Item2 == string.Empty) ||
                    (matches[0].Item2 == SpanStartString && matches[1].Item2 == NamedSpanEndString && spanStartStack.Peek().Item2 != string.Empty))
                {
                    orderedMatches.RemoveAt(0);
                }
            }

            // Order the matches by their index
            var firstMatch = orderedMatches.First();

            var matchIndexInInput = firstMatch.Item1;
            var matchString = firstMatch.Item2;

            var matchIndexInOutput = matchIndexInInput - inputOutputOffset;
            outputBuilder.Append(input.Substring(currentIndexInInput, matchIndexInInput - currentIndexInInput));

            currentIndexInInput = matchIndexInInput + matchString.Length;
            inputOutputOffset += matchString.Length;

            switch (matchString.Substring(0, 2))
            {
                case PositionString:
                    if (position.HasValue)
                    {
                        throw new ArgumentException($"Saw multiple occurrences of {PositionString}");
                    }

                    position = matchIndexInOutput;
                    break;

                case SpanStartString:
                    spanStartStack.Push(Tuple.Create(matchIndexInOutput, string.Empty));
                    break;

                case SpanEndString:
                    if (spanStartStack.Count == 0)
                    {
                        throw new ArgumentException($"Saw {SpanEndString} without matching {SpanStartString}");
                    }

                    if (spanStartStack.Peek().Item2.Length > 0)
                    {
                        throw new ArgumentException(
                            $"Saw {NamedSpanStartString} without matching {NamedSpanEndString}");
                    }

                    PopSpan(spanStartStack, tempSpans, matchIndexInOutput);
                    break;

                case NamedSpanStartString:
                    var name = namedSpanStartMatch.Groups[1].Value;
                    spanStartStack.Push(Tuple.Create(matchIndexInOutput, name));
                    break;

                case NamedSpanEndString:
                    if (spanStartStack.Count == 0)
                    {
                        throw new ArgumentException(
                            $"Saw {NamedSpanEndString} without matching {NamedSpanStartString}");
                    }

                    if (spanStartStack.Peek().Item2.Length == 0)
                    {
                        throw new ArgumentException($"Saw {SpanStartString} without matching {SpanEndString}");
                    }

                    PopSpan(spanStartStack, tempSpans, matchIndexInOutput);
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        if (spanStartStack.Count > 0)
        {
            throw new ArgumentException($"Saw {SpanStartString} without matching {SpanEndString}");
        }

        // Append the remainder of the string.
        outputBuilder.Append(input.Substring(currentIndexInInput));
        output = outputBuilder.ToString();
        spans = tempSpans.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static V GetOrAdd<K, V>(IDictionary<K, V> dictionary, K key, Func<K, V> function)
    {
        if (!dictionary.TryGetValue(key, out var value))
        {
            value = function(key);
            dictionary.Add(key, value);
        }

        return value;
    }

    private static void PopSpan(
        Stack<Tuple<int, string>> spanStartStack,
        IDictionary<string, List<TextSpan>> spans,
        int finalIndex)
    {
        var spanStartTuple = spanStartStack.Pop();

        var span = TextSpan.FromBounds(spanStartTuple.Item1, finalIndex);
        GetOrAdd(spans, spanStartTuple.Item2, _ =>
        {
            var list = new List<TextSpan>();
            return list;
        }).Add(span);
    }

    private static void AddMatch(string input, string value, int currentIndex, List<Tuple<int, string>> matches)
    {
        var index = input.IndexOf(value, currentIndex, StringComparison.Ordinal);
        if (index >= 0)
        {
            matches.Add(Tuple.Create(index, value));
        }
    }

    private static void GetPositionAndSpans(
        string input,
        out string output,
        out int? cursorPositionOpt,
        out ImmutableArray<TextSpan> spans)
    {
        Parse(input, out output, out cursorPositionOpt, out var dictionary);

        var builder = GetOrAdd(dictionary, string.Empty, _ => new List<TextSpan>());
        spans = builder.ToImmutableArray();
    }

    public static void GetPositionAndNamedSpans(
        string input,
        out string output,
        out int? cursorPositionOpt,
        out IDictionary<string, ImmutableArray<TextSpan>> spans)
    {
        Parse(input, out output, out cursorPositionOpt, out var dictionary);
        spans = dictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray());
    }

    public static void GetNamedSpans(string input, out string output, out IDictionary<string, ImmutableArray<TextSpan>> spans)
        => GetPositionAndNamedSpans(input, out output, out var cursorPositionOpt, out spans);

    public static void GetPosition(string input, out string output, out int? cursorPosition)
        => GetPositionAndSpans(input, out output, out cursorPosition, out ImmutableArray<TextSpan> spans);

    public static void GetLineAndColumn(string input, out string output, out int line, out int column)
    {
        GetPosition(input, out output, out var position);
        line = 0;
        column = 0;
        for (int i = 0; i < position; i++)
        {
            switch (output[i])
            {
                case '\n':
                    line++;
                    column = 0;
                    break;
                default:
                    column++;
                    break;
            }
        }
    }
    
    public static void GetLineAndColumn(string code, int position, out int line, out int column)
    {
        column = 0;
        line = 0;
        for (var i = 0; i < position; i++)
        {
            switch (code[i])
            {
                case '\n':
                    line++;
                    column = 0;
                    break;
                default:
                    column++;
                    break;
            }
        }
    }

    public static void GetPositionAndSpan(string input, out string output, out int? cursorPosition, out TextSpan? textSpan)
    {
        GetPositionAndSpans(input, out output, out cursorPosition, out ImmutableArray<TextSpan> spans);
        textSpan = spans.Length == 0 ? null : spans.SingleOrDefault();
    }

    public static void GetSpans(string input, out string output, out ImmutableArray<TextSpan> spans)
    {
        GetPositionAndSpans(input, out output, out int? pos, out spans);
    }

    public static void GetSpan(string input, out string output, out TextSpan textSpan)
    {
        GetSpans(input, out output, out ImmutableArray<TextSpan> spans);
        textSpan = spans.Single();
    }

    private class MatchComparer : IComparer<Tuple<int, string>>
    {
        public static readonly IComparer<Tuple<int, string>> Instance = new MatchComparer();

        private MatchComparer()
        {
        }

        public int Compare(Tuple<int, string> x, Tuple<int, string> y)
        {
            return x.Item1 - y.Item1;
        }
    }
}