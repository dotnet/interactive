// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.IO;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class StdIOConnectionOptions : KernelConnectionOptions
    {
        public string[] Command { get; set; } = new string[0];
        public DirectoryInfo? WorkingDirectory { get; set; }
        public bool WaitForKernelReadyEvent { get; set; }
    }
}
