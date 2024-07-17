// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

internal class DirectiveSyntaxSpec : SyntaxSpecBase<DirectiveNode>
{
    public DirectiveSyntaxSpec(string text, params Action<DirectiveNode>[] assertions) : base(text, assertions)
    {
    }
}