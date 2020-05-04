// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class SupportedDirectives
    {
        public string KernelName { get; }

        public SupportedDirectives(string kernelName)
        {
            KernelName = kernelName;
        }

        public List<ICommand> Commands { get; } = new List<ICommand>();
    }
}