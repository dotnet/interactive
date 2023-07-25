using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.MSBuildTools
{
    /// <summary>
    /// Representation of a .NET project making it useful for various project
    /// analysis and manipulation tasks.
    /// </summary>
    public class MyAnalyzerResult
    {
        public Microsoft.Build.Evaluation.Project Project { get; set; }

        public Microsoft.CodeAnalysis.Workspace RoslynWorkspace { get; set; }

        public bool Succeeded { get; set; }
        public List<string> References { get; set; }
        public List<string> ProjectReferences { get; set; }
    }
}
