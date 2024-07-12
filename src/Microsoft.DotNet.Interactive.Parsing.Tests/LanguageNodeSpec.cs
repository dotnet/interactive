// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

internal class LanguageNodeSpec : SyntaxSpecBase<LanguageNode>
{
    public LanguageNodeSpec(string text, string expectedTargetKernelName, params Action<LanguageNode>[] assertions) : base(text, assertions)
    {
        ExpectedTargetKernelName = expectedTargetKernelName;
    }

    public string ExpectedTargetKernelName { get; }

    public override void Validate(LanguageNode node)
    {
        base.Validate(node);

        node.TargetKernelName.Should().Be(ExpectedTargetKernelName);
    }
}