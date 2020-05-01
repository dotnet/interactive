// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    public class Diagnostic
    {
        internal Diagnostic(
            string message,
            DiagnosticSeverity severity, 
            Location location)
        {
            Message = message;
            Severity = severity;
            Location = location;
        }

        public string Message { get; }

        public DiagnosticSeverity Severity { get; }

        public Location Location { get; }
    }
}