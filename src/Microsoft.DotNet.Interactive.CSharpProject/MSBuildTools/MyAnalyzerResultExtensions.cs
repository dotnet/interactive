using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.MSBuildTools
{
    /// <summary>
    /// The MyAnalyzerResultExtensions class provides extension methods to augment the functionality 
    /// of the MyAnalyzerResult class. These methods add the ability to retrieve parse options, compile 
    /// inputs, and workspaces from MyAnalyzerResult instances.
    /// </summary>
    public static class MyAnalyzerResultExtensions
    {
        /// <summary>
        /// Retrieves the C# parse options for the given MyAnalyzerResult instance. 
        /// These parse options include preprocessor symbols and language version 
        /// based on the properties of the project associated with the instance.
        /// </summary>
        /// <param name="myAnalyzerResult">The MyAnalyzerResult instance to retrieve parse options for.</param>
        /// <returns>A CSharpParseOptions object containing parse options for the instance.</returns>
        public static CSharpParseOptions GetCSharpParseOptions(this MyAnalyzerResult myAnalyzerResult)
        {
            var parseOptions = new CSharpParseOptions();

            // Add any constants
            var constants = myAnalyzerResult.Project.GetProperty("DefineConstants")?.EvaluatedValue;
            if (!string.IsNullOrWhiteSpace(constants))
            {
                parseOptions = parseOptions
                    .WithPreprocessorSymbols(constants.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()));
            }

            // Get language version
            var langVersion = myAnalyzerResult.Project.GetProperty("LangVersion")?.EvaluatedValue;
            if (!string.IsNullOrWhiteSpace(langVersion)
                && LanguageVersionFacts.TryParse(langVersion, out var languageVersion))
            {
                parseOptions = parseOptions.WithLanguageVersion(languageVersion);
            }

            return parseOptions;
        }

        /// <summary>
        /// Retrieves the compile inputs for the given MyAnalyzerResult instance. 
        /// These compile inputs are the file paths of all the source code files in the project.
        /// </summary>
        /// <param name="myAnalyzerResult">The MyAnalyzerResult instance to retrieve compile inputs for.</param>
        /// <returns>An array of strings representing the paths of the source code files in the project.</returns>
        public static string[] GetCompileInputs(this MyAnalyzerResult myAnalyzerResult)
        {
            var projectDirectory = Path.GetDirectoryName(myAnalyzerResult.Project.FullPath);
            var inputFiles = myAnalyzerResult.Project.GetItems("Compile").Select(i => i.EvaluatedInclude);
            var files = inputFiles.Select(pi => Path.Combine(projectDirectory, pi)).ToArray();

            return files;
        }

        /// <summary>
        /// Attempts to create and retrieve a Roslyn workspace for the given MyAnalyzerResult instance. 
        /// The workspace is created based on the project associated with the instance.
        /// </summary>
        /// <param name="myAnalyzerResult">The MyAnalyzerResult instance to retrieve a workspace for.</param>
        /// <param name="ws">Output parameter that will contain the created workspace if the method returns true.</param>
        /// <returns>True if a workspace was successfully created and can be used to generate a compilation; otherwise, false.</returns>
        public static bool TryGetWorkspace(this MyAnalyzerResult myAnalyzerResult, out Microsoft.CodeAnalysis.Workspace ws)
        {
            ws = null;
            try
            {
                var workspace = MSBuildWorkspace.Create();
                var roslynProject = workspace.OpenProjectAsync(myAnalyzerResult.Project.FullPath).Result;
                ws = workspace;
                return ws.CanBeUsedToGenerateCompilation();
            }
            catch
            {
                return false;
            }
        }
    }
}
