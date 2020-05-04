// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Utility
{
    public class AddPackageResult : CommandLineResult
    {
        public AddPackageResult(int exitCode, IReadOnlyCollection<string> output = null, IReadOnlyCollection<string> error = null) : base(exitCode, output, error)
        {
            if (exitCode == 0)
            {
                InstalledVersion = GetInstalledVersion(output);
            }

            DetailedErrors = GetDetailedErrors(output, error);
        }

        public string InstalledVersion { get; }

        public IEnumerable<string> DetailedErrors { get; }

        private IEnumerable<string> GetDetailedErrors(IReadOnlyCollection<string> output, IReadOnlyCollection<string> error)
        {
            return output.Concat(error).Where(l => l.StartsWith("error:"));
        }

        private string GetInstalledVersion(IReadOnlyCollection<string> output)
        {
            foreach (var line in output)
            {
                if (line.StartsWith("info : PackageReference for package"))
                {
                    // Successful installation will print a message like
                    // @"info : PackageReference for package 'xunit' version '2.4.1' added to file 'E:\testdll\testdll.csproj'.";
                    var quote1 = line.IndexOf('\'');
                    if (quote1 == -1)
                    {
                        continue;
                    }

                    var quote2 = line.IndexOf('\'', quote1 + 1);
                    var quote3 = line.IndexOf('\'', quote2 + 1);
                    var quote4 = line.IndexOf('\'', quote3 + 1);
                    var start = quote3 + 1;
                    var length = quote4 - start;
                    return line.Substring(start, length);
                }
            }

            return "";
        }
    }
}