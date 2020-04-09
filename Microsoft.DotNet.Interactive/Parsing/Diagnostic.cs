// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    public class Diagnostic
    {
        internal Diagnostic(DiagnosticSeverity severity, Location location)
        {
            Severity = severity;
            Location = location;
        }

        public DiagnosticSeverity Severity { get; }

        public Location Location { get; }
    }
}