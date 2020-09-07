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

            var decompiler = new ICSharpCode.Decompiler.CSharp.CSharpDecompiler(module: assembly, assemblyResolver: new UniversalAssemblyResolver($"{Defaults.InternalAssemblyName}.dll", true, "netcoreapp3.1"), new DecompilerSettings(decompilationLanguageVersion))
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
