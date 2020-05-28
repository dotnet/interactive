// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.App.CommandLine
{
    public class NamedPipeServerOptions
    {
        public NamedPipeServerOptions(string defaultKernel, string pipeName)
        {
            DefaultKernel = defaultKernel;
            PipeName = pipeName;
        }

        public string DefaultKernel { get; }

        public string PipeName { get; }
    }
}
