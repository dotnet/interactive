// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Parsing
{
    public class LanguageSpecificParseResult
    {
        public static LanguageSpecificParseResult None { get; } = new NoLanguageSpecificParseResult();

        public virtual IEnumerable<Diagnostic> GetDiagnostics()
        {
            yield break;
        }

        private class NoLanguageSpecificParseResult : LanguageSpecificParseResult
        {
            public override IEnumerable<Diagnostic> GetDiagnostics()
            {
                yield break;
            }
        }
    }
}