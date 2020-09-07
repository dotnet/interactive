// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector
{
    public sealed class InspectionResult
    {
        public bool IsSuccess { get; set; } = false;
        public IEnumerable<string> CompilationDiagnostics { get; internal set; } = Enumerable.Empty<string>();
        public string CSharpDecompilation { get; internal set; } = string.Empty;
        public string ILDecompilation { get; internal set; } = string.Empty;
        public string JitDecompilation { get; internal set; } = string.Empty;

        public override string ToString() =>
            new StringBuilder()
                .Append("Diagnostics:\n")
                .Append(string.Join('\n', this.CompilationDiagnostics))
                .Append("C# Decompilation:\n")
                .Append(this.CSharpDecompilation)
                .Append("IL Decompilation:\n")
                .Append(this.ILDecompilation)
                .Append("JIT Asm:\n")
                .Append(this.JitDecompilation)
                .ToString();
    }
}
