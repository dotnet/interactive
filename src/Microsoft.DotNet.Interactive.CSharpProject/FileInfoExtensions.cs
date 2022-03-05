// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tools
{
    public static class FileInfoExtensions
    {
        public static string Read(this FileInfo file)
        {
            using (var reader = file.OpenText())
            {
                return reader.ReadToEnd();
            }
        }

        public static async Task<string> ReadAsync(this FileInfo file)
        {
            using (var reader = file.OpenText())
            {
                return await reader.ReadToEndAsync();
            }
        }

        public static bool IsBuildOutput(this FileInfo fileInfo)
        {
            var directory = fileInfo.Directory;

            while (directory != null)
            {
                if (directory.Name == "obj" || directory.Name == "bin")
                {
                    return true;
                }

                directory = directory.Parent;
            }

            return false;
        }
    }
}
