using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.Build.Locator;

namespace Microsoft.DotNet.Interactive.CSharpProject.MSBuildTools
{
    /// <summary>
    /// Provides functionality to analyze a .NET project file
    /// </summary>
    public class MyBuildalyzer
    {
        /// <summary>
        /// Analyzes the provided project file path using MSBuild for project data and
        /// Roslyn for workspace and compilation data.
        /// </summary>
        /// <param name="projectFilePath"></param>
        /// <returns></returns>
        public MyAnalyzerResult Analyze(string projectFilePath)
        {
            var instances = MSBuildLocator.QueryVisualStudioInstances();
            var instance = instances.OrderByDescending(x => x.Version).First();
            MSBuildLocator.RegisterInstance(instance);

            var project = new Microsoft.Build.Evaluation.Project(projectFilePath);
            var references = project.GetItems("Reference").Select(i => i.EvaluatedInclude).ToList();
            var projectReferences = project.GetItems("ProjectReference").Select(i => i.EvaluatedInclude).ToList();

            MSBuildWorkspace workspace = null;
            try
            {
                workspace = MSBuildWorkspace.Create();
                var roslynProject = workspace.OpenProjectAsync(projectFilePath).Result;
            }
            catch (System.Exception ex)
            {
                // Handle exceptions accordingly
            }

            return new MyAnalyzerResult
            {
                Project = project,
                References = references,
                ProjectReferences = projectReferences,
                RoslynWorkspace = workspace,
                Succeeded = project != null && workspace != null
            };
        }
    }
}
