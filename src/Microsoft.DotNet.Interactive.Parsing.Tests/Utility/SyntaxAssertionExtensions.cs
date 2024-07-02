// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Primitives;

namespace Microsoft.DotNet.Interactive.Parsing.Tests.Utility;

internal static class SyntaxAssertionExtensions
{
    public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
        this GenericCollectionAssertions<SyntaxNode> source,
        Func<T, bool> where = null)
        where T : SyntaxNode
    {
        T subject;

        if (where is null)
        {
            source.ContainSingle(e => e is T);

            subject = source.Subject
                            .OfType<T>()
                            .Single();
        }
        else
        {
            source.ContainSingle(e => e is T && where((T)e));

            subject = source.Subject
                            .OfType<T>()
                            .Where(where)
                            .Single();
        }

        return new AndWhichConstraint<ObjectAssertions, T>(subject.Should(), subject);
    }

    public static AndWhichConstraint<ObjectAssertions, T> ContainSingle<T>(
        this GenericCollectionAssertions<SyntaxNodeOrToken> source,
        Func<T, bool> where = null)
        where T : SyntaxNode
    {
        T subject;

        if (where is null)
        {
            source.ContainSingle(e => e is T);

            subject = source.Subject
                            .OfType<T>()
                            .Single();
        }
        else
        {
            source.ContainSingle(e => e is T && where((T)e));

            subject = source.Subject
                            .OfType<T>()
                            .Where(where)
                            .Single();
        }

        return new AndWhichConstraint<ObjectAssertions, T>(subject.Should(), subject);
    }
}