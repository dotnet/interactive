// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Collections;
using System.CommandLine.Parsing;
using System.Linq;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Parsing;

internal static class ParseResultExtensions
{
    internal static Symbol GetByAlias(this SymbolSet symbolSet, string alias)
        => symbolSet.SingleOrDefault(symbol => symbol.Name.Equals(alias) || symbol is IdentifierSymbol id && id.HasAlias(alias));

    internal static Argument<PackageReferenceOrFileInfo> FindPackageArgument(this Parser parser)
    {
        return parser
               .Configuration
               .Symbols
               .OfType<Command>()
               .FlattenBreadthFirst(s => s.Children.OfType<Command>())
               .SelectMany(c => c.Arguments)
               .ToArray()
               .OfType<Argument<PackageReferenceOrFileInfo>>()
               .SingleOrDefault() ?? throw new ArgumentException($"{nameof(parser)} does not contain a --package argument");
    }
}