// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector.CSharpCompiler
{
    internal readonly ref struct CSharpCompilationResult
    {
        internal readonly bool IsSuccess;
        internal readonly MemoryStream Dll;
        internal readonly MemoryStream Pdb;
        internal readonly ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> Diagnostics;
        public CSharpCompilationResult(bool isSuccess, MemoryStream dll, MemoryStream pdb, ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics)
        {
            this.IsSuccess = isSuccess;
            this.Dll = dll;
            this.Pdb = pdb;
            this.Diagnostics = diagnostics;
        }
    }
}
