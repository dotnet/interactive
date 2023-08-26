using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities.RoslynWorkspaceUtilities;

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

    internal Dictionary<string, BuildDataProjectItem[]> Items { get; } = new Dictionary<string, BuildDataProjectItem[]>();
}
