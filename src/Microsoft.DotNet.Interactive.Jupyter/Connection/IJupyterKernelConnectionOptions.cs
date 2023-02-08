using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

public interface IJupyterKernelConnectionOptions
{
    IReadOnlyCollection<Option> GetOptions();

    IJupyterConnection GetConnection(ParseResult connectionOptionsParseResult);
}
