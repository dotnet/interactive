using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities;

public class BuildProjectData
{
    public Guid ProjectGuid { get; set; } = Guid.Empty;
    public IReadOnlyList<string> ProjectReferences { get; set; } = new List<string>();
    public string ProjectFilePath { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string[] SourceFiles { get; set; } = new string[0];
    public string[] References { get; set; } = new string[0];
    public string[] AnalyzerReferences { get; set; } = new string[0];
    public string[] PreprocessorSymbols { get; set; } = new string[0];
    public string LangVersion { get; set; } = string.Empty;
    public string OutputType { get; set; } = string.Empty;
    // TODO : Modify the target to get this value
    public string DefineConstants { get; set; } = string.Empty;
    public string[] CompileInputs { get; set; } = new string[0];
}
