// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharpProject.Markdown;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class AnnotatedCodeBlockTests
{
    [Fact]
    public void It_requires_options_to_initialize()
    {
        var block = new AnnotatedCodeBlock();

        block.Invoking(b => b.InitializeAsync().Wait())
            .Should()
            .Throw<InvalidOperationException>()
            .And
            .Message
            .Should()
            .Be("Attempted to initialize block before parsing code fence annotations");
    }
}