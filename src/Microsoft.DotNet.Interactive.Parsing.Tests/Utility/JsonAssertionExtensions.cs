// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Newtonsoft.Json.Linq;
using JsonDiffPatchDotNet;

namespace Microsoft.DotNet.Interactive.Parsing.Tests.Utility;

internal static class JsonAssertionExtensions
{
    public static AndWhichConstraint<StringAssertions, string> BeEquivalentJsonTo(
        this StringAssertions source,
        string expected)
    {
        var subject = source.Subject;

        var actualJson = JToken.Parse(subject);
        var expectedJson = JToken.Parse(expected);

        if (!JToken.DeepEquals(actualJson, expectedJson))
        {
            var jdp = new JsonDiffPatch();

            var diff = jdp.Diff(expectedJson, actualJson);
            var diffJson = diff.ToString();

            throw new AssertionFailedException($"""
                JSON is not equivalent.

                --------------------------------------
                DIFF:
                {diffJson}

                --------------------------------------
                EXPECTED:
                {expectedJson}

                --------------------------------------
                ACTUAL:
                {actualJson}
                """);

            // Output:
            // {
            //     "key": [false, true]
            // }
        }

        return new AndWhichConstraint<StringAssertions, string>(
            subject.Should(),
            subject);
    }
}