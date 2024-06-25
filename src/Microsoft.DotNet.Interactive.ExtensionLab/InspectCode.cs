// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.ExtensionLab;

public class InspectCode : KernelCommand
{
    public OptimizationLevel Configuration { get; set; }
    public SourceCodeKind Kind { get; set; }
    public Platform Platform { get; set; }
}