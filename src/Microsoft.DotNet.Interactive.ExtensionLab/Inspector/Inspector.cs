using System;
using System.IO;
using System.Linq;
using System.Threading;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector
{
    public sealed class Inspector
    {
        private InspectionOptions Options { get; set; }

        private Inspector(InspectionOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            this.Options = options;
        }

        public static Inspector Create(InspectionOptions options) => new Inspector(options);

        // Pretty simple PoC, types and control flow can be organized better.
        // Specifically, error reporting, by using results, right now it will throw in most of cases if something goes wrong).
        // Now, mostly everything is returned as strings.
        public InspectionResult Compile(string? source) {

            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Argument is null or empty.", nameof(source));

            // Compile, get assembly dll, pdb and diagnostics.
            var csharpCompilationResult = CSharpCompiler.CSharpCompiler.Compile(source, this.Options);

            if (!csharpCompilationResult.IsSuccess)
                return new InspectionResult
                {
                    IsSuccess = false,
                    CompilationDiagnostics = csharpCompilationResult.Diagnostics?.Select(d => d.ToString()) ?? Enumerable.Empty<string>()
                };
            using var dll = csharpCompilationResult.Dll;
            using var pdb = csharpCompilationResult.Pdb;
            using var debugInfoProvider = new PdbDebugInfoProvider(pdb);
            using var assembly = new PEFile($"{Defaults.InternalAssemblyName}.dll", dll);

            // Decompile to "generated" C#
            var csharpDecompilation = CSharpDecompiler.CSharpDecompiler.Decompile(assembly, this.Options, debugInfoProvider);

            // Decompile to IL
            var ilDecompilation = ILDecompiler.ILDecompiler.Decompile(assembly, debugInfoProvider);

            // Get JIT Asm
            var jitDecompilation = JitAsmDecompiler.JitAsmDecompiler.Decompile(dll);

            return new InspectionResult
            {
                IsSuccess = true,
                CompilationDiagnostics = csharpCompilationResult.Diagnostics?.Select(d => d.ToString()) ?? Enumerable.Empty<string>(),
                CSharpDecompilation = csharpDecompilation,
                ILDecompilation = ilDecompilation,
                JitDecompilation = jitDecompilation
            };
        }
    }
}