// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;

namespace Microsoft.DotNet.Interactive.Documents.Tests;

[TestClass]
public class KernelInfoCollectionTests
{
    [TestMethod]
    public void When_a_KernelInfo_with_a_conflicting_name_is_added_then_it_throws_()
    {
        var collection = new KernelInfoCollection();

        collection.Add(new("one"));

        collection.Invoking(c => c.Add(new("one")))
                  .Should()
                  .Throw<ArgumentException>()
                  .Which
                  .Message
                  .Should()
                  .Be($"A {nameof(KernelInfo)} with name or alias 'one' is already present in the collection.");
    }

    [TestMethod]
    public void When_a_KernelInfo_with_a_conflicting_alias_is_added_then_it_throws_()
    {
        var collection = new KernelInfoCollection();

        collection.Add(new("one"));

        collection.Invoking(c => c.Add(new("two", aliases: new[] { "one" })))
                  .Should()
                  .Throw<ArgumentException>()
                  .Which
                  .Message
                  .Should()
                  .Be($"A {nameof(KernelInfo)} with name or alias 'one' is already present in the collection.");
    }

    [TestMethod]
    public void When_an_item_is_removed_then_Contains_no_longer_includes_its_name()
    {
        var collection = new KernelInfoCollection();

        var kernelInfo = new KernelInfo("a", aliases: new[] { "b" });

        collection.Add(kernelInfo);

        collection.Remove(kernelInfo);

        collection.Contains("a").Should().BeFalse();
    }

    [TestMethod]
    public void When_an_item_is_removed_then_Contains_no_longer_includes_its_aliases()
    {
        var collection = new KernelInfoCollection();

        var kernelInfo = new KernelInfo("a", aliases: new[] { "b" });

        collection.Add(kernelInfo);

        collection.Remove(kernelInfo);

        collection.Contains("b").Should().BeFalse();
    }
}
