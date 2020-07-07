// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace Microsoft.DotNet.Interactive.Parsing
{
    internal static class LanguageNodeExtensions
    {
        public static bool IsUnknownActionDirective(this DirectiveNode directiveNode)
        {
            var parseResult = directiveNode.GetDirectiveParseResult();
            return parseResult.Errors.All(e => e.SymbolResult == null) && directiveNode is ActionDirectiveNode;
        }

    }
}