// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.DotNet.Interactive.App.CommandLine
{
    public class JupyterOptions
    {
        public JupyterOptions(FileInfo connectionFile, string defaultKernel)
        {
            ConnectionFile = connectionFile;
            DefaultKernel = defaultKernel;
        }

        public FileInfo ConnectionFile { get; }

        public string DefaultKernel { get; }
    }
}
