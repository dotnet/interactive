using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive
{
    public class Project
    {
        public IReadOnlyCollection<ProjectFile> Files { get; }

        public Project(IReadOnlyCollection<ProjectFile> files)
        {
            Files = files;
        }
    }
}
