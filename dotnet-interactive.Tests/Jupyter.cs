// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.App.Tests
{
    internal static class Jupyter
    {
        private static bool? _isInstalled;
        public static bool IsInstalled => _isInstalled ??= CheckIsInstalled();

        private static bool CheckIsInstalled()
        {
            var expectedPath = JupyterKernelSpecInstaller.GetDefaultDirectory();

            return expectedPath.Exists;
        }
    }
}