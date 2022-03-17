// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation
{
    using LinePosition = CodeAnalysis.Text.LinePosition;
    using LinePositionSpan = CodeAnalysis.Text.LinePositionSpan;

    public class VariableLocation 
    {
        public ISymbol Variable { get; }
        public int StartLine { get; }
        public int EndLine { get; }
        public int StartColumn { get; }
        public int EndColumn { get; }

        public VariableLocation(ISymbol variable, int startLine, int endLine, int startColumn, int endColumn)
        {
            Variable = variable;
            StartLine = startLine;
            EndLine = endLine;
            StartColumn = startColumn;
            EndColumn = endColumn;
        }

        public static VariableLocation FromSpan(ISymbol variable, LinePositionSpan span)
        {
            return new VariableLocation(
                variable,
                span.Start.Line,
                startColumn: span.Start.Character,
                endLine: span.End.Line,
                endColumn: span.End.Character);
        }

        public LinePositionSpan ToLinePositionSpan()
        {
            return new LinePositionSpan(
                new LinePosition(StartLine, StartColumn),
                new LinePosition(EndLine, EndColumn));
        }

        public override bool Equals(object obj)
        {
            return obj is VariableLocation location &&
                   EqualityComparer<ISymbol>.Default.Equals(Variable, location.Variable) &&
                   StartLine == location.StartLine &&
                   EndLine == location.EndLine &&
                   StartColumn == location.StartColumn &&
                   EndColumn == location.EndColumn;
        }

        public override int GetHashCode()
        {
            var hashCode = 1032872879;
            hashCode = hashCode * -1521134295 + EqualityComparer<ISymbol>.Default.GetHashCode(Variable);
            hashCode = hashCode * -1521134295 + StartLine.GetHashCode();
            hashCode = hashCode * -1521134295 + EndLine.GetHashCode();
            hashCode = hashCode * -1521134295 + StartColumn.GetHashCode();
            hashCode = hashCode * -1521134295 + EndColumn.GetHashCode();
            return hashCode;
        }

        public string Serialize()
        {
            return $@"
{{
    \""startLine\"": {StartLine},
    \""startColumn\"": {StartColumn},
    \""endLine\"": {EndLine},
    \""endColumn\"": {EndColumn}
}}
";
        }
    }
}
