using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities.RoslynWorkspaceUtilities;

namespace Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities;

public class BuildDataResults
{
    public BuildProjectData BuildProjectData { get; set; }

    public CodeAnalysis.Workspace Workspace { get; set; }

    public bool Succeeded { get; set; }

    public string CacheFilePath { get; set; }

    public CSharpParseOptions CSharpParseOptions { get; set; }

    // TODO: Set this value
    public string ProjectFilePath { get; set; }

    internal Dictionary<string, BuildDataProjectItem[]> Items { get; set; } = new Dictionary<string, BuildDataProjectItem[]>();
}
