// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using ICSharpCode.Decompiler.Metadata;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector.CSharpDecompiler
{
    /// <summary>
    /// This assembly resolver is to work around issue https://github.com/icsharpcode/ILSpy/issues/2228 where assemblies
    /// from the 5.0.0 runtime can't be resolved.
    /// </summary>
    internal class RuntimeAssemblyResolver : IAssemblyResolver
    {
        private DirectoryInfo _runtimeDir;
        private UniversalAssemblyResolver _universalResolver;
        private static string[] _extensions = new string[] { ".exe", ".dll" };

        public RuntimeAssemblyResolver(string mainAssemblyFileName, string targetFramework)
        {
            _runtimeDir = new DirectoryInfo(Path.GetDirectoryName(typeof(object).Assembly.Location));
            _universalResolver = new UniversalAssemblyResolver(mainAssemblyFileName, false, targetFramework);
        }

        public PEFile Resolve(IAssemblyReference reference) => _universalResolver.Resolve(reference) ?? ResolveFile(reference.Name);

        public PEFile ResolveModule(PEFile mainModule, string moduleName) => _universalResolver.ResolveModule(mainModule, moduleName) ?? ResolveFile(mainModule.Name);

        private PEFile ResolveFile(string name)
        {
            foreach (var extension in _extensions)
            {
                var expectedFileName = name + extension;
                var expectedFilePath = Path.Combine(_runtimeDir.FullName, expectedFileName);
                if (File.Exists(expectedFilePath))
                {
                    var stream = File.OpenRead(expectedFilePath);
                    return new PEFile(expectedFileName, stream);
                }
            }

            throw new FileNotFoundException($"Unable to find assembly '{name}'");
        }
    }
}
