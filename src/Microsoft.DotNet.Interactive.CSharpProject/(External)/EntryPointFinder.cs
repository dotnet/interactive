// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class EntryPointFinder : AbstractEntryPointFinder
{
    protected override bool MatchesMainMethodName(string name)
    {
        return name == "Main";
    }

    public static IMethodSymbol FindEntryPoint(INamespaceSymbol symbol)
    {
        var visitor = new EntryPointFinder();
        visitor.Visit(symbol);
        return visitor.EntryPoints.SingleOrDefault();
    }
}