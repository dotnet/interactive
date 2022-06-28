// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using System.Linq;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public static class StringAssertionExtensions
{
    public static void ShouldMatch(this IReadOnlyCollection<string> actual, params string[] expected)
    {
        for (var i = 0; i < expected.Length; i++)
        {
            var item = actual.ElementAtOrDefault(i);

            item.Should().NotBeNull(because: $"expected to match a collection of {expected.Length} lines but only found {i}");
            if (string.IsNullOrEmpty(item))
            {
                item.Should().BeEmpty();
            }
            else
            {
                item.Should().Match(expected[i]);
            }
        }
    }
        
    public static void ShouldMatchLineByLineTrimmed(this IReadOnlyCollection<string> actual, params string[] expected)
        => ShouldMatch(actual.Select(x => x.Trim()).ToList(), expected.Select(x => x.Trim()).ToArray());

    public static void ShouldMatchLineByLine(this string actual, string expected)
        => ShouldMatchLineByLineTrimmed(actual.EnforceLF().Split("\n"), expected.EnforceLF().Split("\n"));
}