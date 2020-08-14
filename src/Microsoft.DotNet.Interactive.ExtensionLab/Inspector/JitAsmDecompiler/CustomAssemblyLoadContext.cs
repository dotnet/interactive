using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector.JitAsmDecompiler
{
    internal sealed class CustomAssemblyLoadContext : AssemblyLoadContext, IDisposable
    {
        private readonly Func<AssemblyName, bool> _shouldShareAssembly;

        public CustomAssemblyLoadContext(Func<AssemblyName, bool> shouldShareAssembly)
            : base(isCollectible: true) => _shouldShareAssembly = shouldShareAssembly;

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var name = assemblyName.Name ?? "";
            if (name == "netstandard" || name == "mscorlib" || name.StartsWith("System.") || _shouldShareAssembly(assemblyName))
                return Assembly.Load(assemblyName);

            return LoadFromAssemblyPath(Path.Combine(AppContext.BaseDirectory, assemblyName.Name + ".dll"));
        }

        public void Dispose() => Unload();
    }
}
