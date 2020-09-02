
using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Inspector.Extensions
{
    internal static class TypeExtensions
    {
        public static string GetAssemblyLoadPath(this System.Type type) => type?.Assembly?.Location ?? throw new Exception("Couldn't get assembly location path.");
        public static string GetSystemAssemblyPathByName(string assemblyName) => Path.Combine(Path.GetDirectoryName(typeof(object).GetAssemblyLoadPath()), assemblyName);
    }
}
