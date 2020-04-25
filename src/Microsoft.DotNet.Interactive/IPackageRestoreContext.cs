using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive
{
    public interface IPackageRestoreContext
    {
        public IEnumerable<string> RestoreSources { get; }

        public IEnumerable<PackageReference> RequestedPackageReferences { get; }

        public IEnumerable<ResolvedPackageReference> ResolvedPackageReferences { get; }

    }
}
