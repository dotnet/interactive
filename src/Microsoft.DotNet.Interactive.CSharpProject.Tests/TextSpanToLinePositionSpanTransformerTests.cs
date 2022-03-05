// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests
{
    public class TextSpanToLinePositionSpanTransformerTests
    {
        [Fact]
        public void It_Should_Convert_Text_Spans()
        {
            var sourceText = SourceText.From(
                ("hello\nworld").EnforceLF()
            );

            var span = new TextSpan(0, 11);
            var newSpan = span.ToLinePositionSpan(sourceText);

            newSpan.Start.Line.Should().Be(0);
            newSpan.Start.Character.Should().Be(0);
            newSpan.End.Line.Should().Be(1);
            newSpan.End.Character.Should().Be(5);
        }
    }
}

