// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    internal static  class JupyterKernelSpecModule
    {
        private static bool? _isOnPAth;

        public static bool IsOnPath => _isOnPAth ??= CheckIsOnPath();
        private static bool CheckIsOnPath()
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