// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public static class CodeManipulation
{
    public static (string processed, int markLocation) ProcessMarkup(string source)
    {
        // TODO: (ProcessMarkup) remove, use MarkupTestFile instead
        var normalised = source.EnforceLF();
        var markLocation = normalised.IndexOf("$$", StringComparison.InvariantCulture);
        var processed = normalised.Replace("$$", string.Empty);
        return (processed, markLocation);
    }
}