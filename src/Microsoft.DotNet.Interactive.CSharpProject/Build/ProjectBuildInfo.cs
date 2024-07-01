using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.CSharpProject.Build;

internal class ProjectBuildInfo
{
    public Guid ProjectGuid { get; set; } = Guid.Empty;
    public IReadOnlyList<string> ProjectReferences { get; set; } = [];
    public string ProjectFilePath { get; set; } = string.Empty;

    public string LanguageName { get; set; } = string.Empty;

    public string TargetPath { get; set; } = string.Empty;
    public string[] SourceFiles { get; set; } = [];
    public string[] References { get; set; } = [];
    public string[] AnalyzerReferences { get; set; } = [];
    public string[] PreprocessorSymbols { get; set; } = [];
    public string LangVersion { get; set; } = string.Empty;

    public string OutputType { get; set; } = string.Empty;

    // TODO : Modify the target to get this value
    public string DefineConstants { get; set; } = string.Empty;
    public string[] CompileInputs { get; set; } = [];
}