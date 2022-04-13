// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Buildalyzer.Workspaces;
using System.Linq;
using System.Runtime.CompilerServices;
using Buildalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;

//adpated from https://github.com/daveaglick/Buildalyzer/blob/master/src/Buildalyzer.Workspaces/AnalyzerResultExtensions.cs

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

public static class AnalyzerResultExtensions
{
    private static readonly ConditionalWeakTable<IAnalyzerResult, string[]> CompilerInputs = new();

    public static CSharpParseOptions GetCSharpParseOptions(this IAnalyzerResult analyzerResult)
    {
        var parseOptions = new CSharpParseOptions();

        // Add any constants
        var constants = analyzerResult.GetProperty("DefineConstants");
        if (!string.IsNullOrWhiteSpace(constants))
        {
            parseOptions = parseOptions
                .WithPreprocessorSymbols(constants.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()));
        }

        // Get language version
        var langVersion = analyzerResult.GetProperty("LangVersion");
        if (!string.IsNullOrWhiteSpace(langVersion)
            && LanguageVersionFacts.TryParse(langVersion, out var languageVersion))
        {
            parseOptions = parseOptions.WithLanguageVersion(languageVersion);
        }

        return parseOptions;
    }

    public static string[] GetCompileInputs(this IAnalyzerResult analyzerResult)
    {
        string[] files;
        lock (CompilerInputs)
        {
            if (!CompilerInputs.TryGetValue(analyzerResult, out files))
            {
                var projectDirectory = Path.GetDirectoryName(analyzerResult.ProjectFilePath);
                var found = analyzerResult.Items.TryGetValue("Compile", out var inputFiles);
                files = found ? inputFiles.Select(pi => Path.Combine(projectDirectory, pi.ItemSpec)).ToArray() : Array.Empty<string>();
                CompilerInputs.Add(analyzerResult, files);
            }
        }

        return files;
    }

    internal static bool TryGetWorkspace(
        this IAnalyzerResult analyzerResult,
        out Workspace ws)
    {
        ws = analyzerResult.GetWorkspace();
        return ws.CanBeUsedToGenerateCompilation();
    }
}