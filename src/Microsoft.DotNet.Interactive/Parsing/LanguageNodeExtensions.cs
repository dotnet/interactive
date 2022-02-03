// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Parsing
{
    internal static class LanguageNodeExtensions
    {
        public static bool IsUnknownActionDirective(this DirectiveNode directiveNode)
        {
            return directiveNode is ActionDirectiveNode &&
                   directiveNode.GetDirectiveParseResult().Errors.All(e => e.SymbolResult?.Symbol is RootCommand);
        }
    }
}