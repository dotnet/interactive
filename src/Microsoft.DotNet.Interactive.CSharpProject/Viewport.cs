// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class Viewport
{
    public Viewport(SourceFile destination, TextSpan region, TextSpan outerRegion, BufferId bufferId)
    {

        Region = region;
        BufferId = bufferId;
        OuterRegion = outerRegion;
        Destination = destination ?? throw new ArgumentNullException(nameof(destination));
    }

    public BufferId BufferId { get; }

    public TextSpan OuterRegion { get; }

    public TextSpan Region { get; }

    public SourceFile Destination { get; }
}