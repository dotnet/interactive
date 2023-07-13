using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal interface ITextSnapshot
{
    IEnumerable<ITextSnapshotLine> Lines { get; }
}
