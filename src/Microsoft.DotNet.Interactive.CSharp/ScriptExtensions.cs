// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace Microsoft.DotNet.Interactive.CSharp
{
    internal static class ScriptExtensions
    {
        public static IEnumerable<Diagnostic> GetDiagnostics(this Script script)
        {
            if(script == null)
            {
                return Enumerable.Empty<Diagnostic>();
            }

            var compilation = script.GetCompilation();
            var orderedDiagnostics = compilation.GetDiagnostics().OrderBy((d1, d2) =>
            {
                var severityDiff = (int)d2.Severity - (int)d1.Severity;
                return severityDiff != 0 ? severityDiff : d1.Location.SourceSpan.Start - d2.Location.SourceSpan.Start;
            });

            return orderedDiagnostics;
        }
    }
}