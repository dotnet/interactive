// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive
{
    public class Diagnostic
    {
        public Diagnostic(LinePositionSpan linePositionSpan, DiagnosticSeverity severity, string code, string message)
        {
            LinePositionSpan = linePositionSpan;
            Severity = severity;
            Code = code;
            Message = message;
        }

        public LinePositionSpan LinePositionSpan { get; }
        public DiagnosticSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }

        public Diagnostic WithLinePositionSpan(LinePositionSpan linePositionSpan)
        {
            return new Diagnostic(
                linePositionSpan,
                Severity,
                Code,
                Message);
        }

        public override string ToString()
        {
            return $"{Code}: {LinePositionSpan} {Message}";
        }

        public static Diagnostic FromCodeAnalysisDiagnostic(CodeAnalysis.Diagnostic diagnostic)
        {
            var fileLocation = diagnostic.Location.GetLineSpan();
            return new Diagnostic(
                new LinePositionSpan(fileLocation.StartLinePosition, fileLocation.EndLinePosition),
                diagnostic.Severity,
                diagnostic.Id,
                diagnostic.GetMessage());
        }
    }
}
