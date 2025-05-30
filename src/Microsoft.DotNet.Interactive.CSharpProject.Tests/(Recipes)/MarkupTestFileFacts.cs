// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class MarkupTestFileFacts
{
    [Fact]
    public void GetPosition_Should_Return_String_Without_Dollar_Signs()
    {
        var input = "some$$string";
        MarkupTestFile.GetPosition(input, out var output, out var position);
        Assert.Equal("somestring", output);
    }

    [Fact]
    public void GetPosition_Should_Return_Correct_Position()
    {
        var input = "some$$string";
        MarkupTestFile.GetPosition(input, out var output, out var position);
        Assert.Equal(4, position);
    }

    [Fact]
    public void GetSpans_Should_Return_Correct_Spans()
    {
        var input = "[|input span|]other[|second span|]";
        MarkupTestFile.GetSpans(input, out var output, out ImmutableArray<TextSpan> spans);
        var expected = ImmutableArray.Create(
            new TextSpan(0, 10),
            new TextSpan(15, 11)
        );
        Assert.Equal(expected.ToArray(), spans.ToArray());
    }

    [Fact]
    public void GetNamedSpans_Should_Return_Correct_Named_Spans()
    {
        var input = "{|first:input span|}other";
        MarkupTestFile.GetNamedSpans(input, out string output, out IDictionary<string, ImmutableArray<TextSpan>> spans);
        var expected = ImmutableArray.Create(new TextSpan(0, 10));
        Assert.Equal(expected.ToArray(), spans["first"].ToArray());
    }
}