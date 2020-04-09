// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    internal static  class JupyterKernelSpecModule
    {
        public static bool Exists()
        {
            var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "where"
                : "which";

            var jupyterKernelSpecExists = false;

            Task.Run(async () => {
                var result = await Utility.CommandLine.Execute(command, "jupyter-kernelspec");
                jupyterKernelSpecExists = result.ExitCode == 0;
            }).Wait(2000);

            return jupyterKernelSpecExists;
        }
    }
}