using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities;

internal class BuildDataProjectItem
{
    internal string ItemSpec { get; }
    internal IReadOnlyDictionary<string, string> Metadata { get; }

    internal BuildDataProjectItem()
    {
        //            ItemSpec = taskItem.ItemSpec;
        //            Metadata = taskItem.MetadataNames.Cast<string>().ToDictionary(x => x, x => taskItem.GetMetadata(x));
    }
}
