// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Primitives;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests.Utility;

internal static class AssertionExtensions
{
    public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
        this GenericCollectionAssertions<HttpSyntaxNode> should)
        where T : HttpSyntaxNode
    {
        should.ContainSingle(e => e is T);

        var subject = should.Subject
                            .OfType<T>()
                            .Single();

        return new AndWhichConstraint<ObjectAssertions, T>(subject.Should(), subject);
    }
}