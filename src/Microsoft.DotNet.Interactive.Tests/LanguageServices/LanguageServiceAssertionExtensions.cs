// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Tests.LanguageServices;

public static class LanguageServiceAssertionExtensions
{
    public static MarkedUpCode ParseMarkupCode(this string markupCode) => new(markupCode);

    public static IEnumerable<MarkedUpCodeLinePosition> PositionsInMarkedSpans(
        this MarkedUpCode markedUpCode)
    {
        foreach (var position in Enumerable.Range(
                     markedUpCode.Span.Start,
                     markedUpCode.Span.Length + 1))
        {
            var linePosition = LinePosition.FromCodeAnalysisLinePosition(markedUpCode.SourceText.Lines.GetLinePosition(position));

            yield return new MarkedUpCodeLinePosition(markedUpCode, linePosition);
        }
    }

    public static async Task<AndWhichConstraint<
        GenericCollectionAssertions<CompletionsProduced>,
        IEnumerable<CompletionsProduced>>> ProvideCompletionsAsync(
        this GenericCollectionAssertions<MarkedUpCodeLinePosition> assertions,
        Kernel kernel)
    {
        var items = new List<CompletionsProduced>();

        using var _ = new AssertionScope();

        foreach (var position in assertions.Subject)
        {
            var result = await kernel.SendAsync(
                new RequestCompletions(
                    position.MarkedUpCode.Code,
                    position.LinePosition));

            var requestCompleted = result.Events
                                         .Should()
                                         .ContainSingle<CompletionsProduced>()
                                         .Which;

            items.Add(requestCompleted);
        }

        return new AndWhichConstraint<
            GenericCollectionAssertions<CompletionsProduced>,
            IEnumerable<CompletionsProduced>>(
            items.Should(), 
            items);
    }
}

public class MarkedUpCodeLinePosition
{
    public MarkedUpCodeLinePosition(MarkedUpCode markedUpCode, in LinePosition linePosition)
    {
        MarkedUpCode = markedUpCode;
        LinePosition = linePosition;
    }

    public MarkedUpCode MarkedUpCode { get; }

    public LinePosition LinePosition { get; }
}

public class MarkedUpCode
{
    public MarkedUpCode(string markupCode)
    {
        MarkupTestFile.GetSpan(markupCode, out var code, out var span);

        SourceText = SourceText.From(code);

        Code = code;

        Span = span;
    }

    public SourceText SourceText { get; }

    public string Code { get; }

    public TextSpan Span { get; }
}