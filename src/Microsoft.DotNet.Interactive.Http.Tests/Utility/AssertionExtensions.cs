// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Primitives;
using Microsoft.DotNet.Interactive.Http.Parsing;

namespace Microsoft.DotNet.Interactive.Http.Tests.Utility;

internal static class AssertionExtensions
{
    public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
        this GenericCollectionAssertions<SyntaxNode> should)
        where T : SyntaxNode
    {
        should.ContainSingle(e => e is T);

        var subject = should.Subject
                            .OfType<T>()
                            .Single();

        return new AndWhichConstraint<ObjectAssertions, T>(subject.Should(), subject);
    }
    
    public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
        this GenericCollectionAssertions<SyntaxNodeOrToken> should)
        where T : SyntaxNode
    {
        should.ContainSingle(e => e is T);

        var subject = should.Subject
                            .OfType<T>()
                            .Single();

        return new AndWhichConstraint<ObjectAssertions, T>(subject.Should(), subject);
    }
}