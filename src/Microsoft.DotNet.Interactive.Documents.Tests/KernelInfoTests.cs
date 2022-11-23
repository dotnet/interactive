// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Documents.Tests;

public class KernelInfoTests
{
    [Fact]
    public void Aliases_do_not_include_name()
    {
        new KernelInfo("one", aliases: new[] { "two", "three" })
            .Aliases
            .Should()
            .BeEquivalentTo("two", "three");
    }
}
