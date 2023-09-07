// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

public interface IJupyterKernelConnectionOptions
{
    IReadOnlyCollection<Option> GetOptions();

    IJupyterConnection GetConnection(ParseResult connectionOptionsParseResult);
}
