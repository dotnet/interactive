using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.DotNet.Interactive.ExtensionLab.Inspector.Extensions;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector.CSharpCompiler
{
    internal static class CSharpCompiler
    {
        internal static CSharpCompilationResult Compile(string source, in InspectionOptions inspectionOptions)
        {
            var compilationLanguageVersion = Defaults.GetCompilationLanguageVersion(inspectionOptions.CompilationLanguage);

            var options = new CSharpParseOptions(
                languageVersion: compilationLanguageVersion,
                documentationMode: DocumentationMode.Parse,
                inspectionOptions.Kind)
                .WithFeatures(new[] { new KeyValuePair<string, string>("flow-analysis", "") });

            var syntaxTree = CSharpSyntaxTree.ParseText(source, options);

            var compilationOptions = new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: inspectionOptions.OptimizationLevel,
                platform: inspectionOptions.Platform,
                usings: Defaults.DefaultCSharpImports,
                reportSuppressedDiagnostics: true,
                checkOverflow: true,
                allowUnsafe: true);

            var compilation = CSharpCompilation.Create(Defaults.InternalAssemblyName, options: compilationOptions)
                .AddSyntaxTrees(syntaxTree)
                .AddReferences(
                    MetadataReference.CreateFromFile(typeof(object).GetAssemblyLoadPath()),
                    MetadataReference.CreateFromFile(typeof(Console).GetAssemblyLoadPath()),
                    MetadataReference.CreateFromFile(typeof(System.Collections.IEnumerable).GetAssemblyLoadPath()),
                    MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IEnumerable<>).GetAssemblyLoadPath()),
                    MetadataReference.CreateFromFile(TypeExtensions.GetSystemAssemblyPathByName("System.Linq.dll")),
                    MetadataReference.CreateFromFile(TypeExtensions.GetSystemAssemblyPathByName("System.Threading.Tasks.dll")),
                    MetadataReference.CreateFromFile(TypeExtensions.GetSystemAssemblyPathByName("System.Runtime.dll")));

            /*var model = compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
            var methodBodySyntax = syntaxTree.GetCompilationUnitRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Last();
            var cfgFromSyntax = ControlFlowGraph.Create(methodBodySyntax, model);*/

            var dll = new MemoryStream();
            var pdb = new MemoryStream();

            var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb);
            var emitResult = compilation.Emit(peStream: dll, pdbStream: pdb, options: emitOptions);

            if (!emitResult.Success)
                return new CSharpCompilationResult(false, null, null, emitResult.Diagnostics);

            dll.Seek(0, SeekOrigin.Begin);
            pdb.Seek(0, SeekOrigin.Begin);

            return new CSharpCompilationResult(true, dll, pdb, emitResult.Diagnostics);
        }
    }
}
