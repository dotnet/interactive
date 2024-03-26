// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities;

internal class BuildDataResults
{
    internal BuildProjectData BuildProjectData { get; init; }

    internal CodeAnalysis.Workspace Workspace { get; init; }

    internal bool Succeeded { get; init; }

    internal string CacheFilePath { get; init; }

    internal CSharpParseOptions CSharpParseOptions { get; init; }

    // TODO: Set this value
    internal string ProjectFilePath { get; init; }

    internal Dictionary<string, BuildDataProjectItem[]> Items { get; } = new();
}