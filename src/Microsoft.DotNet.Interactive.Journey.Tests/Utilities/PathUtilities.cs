// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;

namespace Microsoft.DotNet.Interactive.Journey.Tests.Utilities
{
    public class PathUtilities
    {
        public static string GetNotebookPath(string notebookName)
        {
            var relativeFilePath = $"Notebooks/{notebookName}";
            var prefix = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            return Path.GetFullPath(prefix is { } ? Path.Combine(prefix, relativeFilePath) : relativeFilePath);
        }
    }
}
