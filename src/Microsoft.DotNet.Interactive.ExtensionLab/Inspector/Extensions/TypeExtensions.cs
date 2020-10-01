// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
