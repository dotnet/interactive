// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

internal class PolyglotSubmissionSyntaxSpec : SyntaxSpecBase<PolyglotSubmissionNode>
{
    private readonly List<ISyntaxSpec> _topLevelSyntaxSpecs = new();

    public PolyglotSubmissionSyntaxSpec(params ISyntaxSpec[] specs)
    {
        _topLevelSyntaxSpecs.AddRange(specs);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        foreach (var spec in _topLevelSyntaxSpecs)
        {
            sb.Append(spec);
            sb.Append(MaybeWhitespace());
            sb.AppendLine();
            sb.Append(MaybeNewLines());
        }

        return sb.ToString();
    }
}