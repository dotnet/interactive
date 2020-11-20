// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Metadata;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector.CSharpDecompiler
{
    internal static class CSharpDecompiler
    {
        internal static string Decompile(in PEFile assembly, in InspectionOptions inspectionOptions, in IDebugInfoProvider debugInfoProvider)
        {
            var decompilationLanguageVersion = Defaults.GetDecompilationLanguageVersion(inspectionOptions.DecompilationLanguage);

            // due to https://github.com/icsharpcode/ILSpy/issues/2228, we need a custom assembly resolver
            var assemblyResolver = new RuntimeAssemblyResolver($"{Defaults.InternalAssemblyName}.dll", "net5.0");
            var settings = new DecompilerSettings(decompilationLanguageVersion);
            var decompiler = new ICSharpCode.Decompiler.CSharp.CSharpDecompiler(module: assembly, assemblyResolver: assemblyResolver, settings: settings)
            {
                DebugInfoProvider = debugInfoProvider
            };

            var decompiledSyntaxTree = decompiler.DecompileWholeModuleAsSingleFile();

            var decompiledCSharpCode = new StringWriter();

            var formattingOptions = FormattingOptionsFactory.CreateAllman();
                formattingOptions.IndentationString = Defaults.DefaultIndent;

            new CSharpOutputVisitor(decompiledCSharpCode, formattingOptions).VisitSyntaxTree(decompiledSyntaxTree);

            return decompiledCSharpCode.ToString();
        }
    }
}
