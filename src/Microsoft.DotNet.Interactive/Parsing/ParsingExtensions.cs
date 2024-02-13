// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Parsing;

internal static class ParseResultExtensions
{
    internal static Symbol GetByAlias(this IEnumerable<Symbol> symbols, string alias)
        => symbols.SingleOrDefault(symbol => symbol.Name.Equals(alias) || symbol is IdentifierSymbol id && id.HasAlias(alias));
}