// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public static class SandDanceExplorerExtensions
    {
        public static T UseSandDanceExplorer<T>(this T kernel, string uri = null, string libraryVersion = null, string cacheBuster = null) where T : Kernel
        {
            SandDanceDataExplorer.RegisterFormatters();
            SandDanceDataExplorer.SetDefaultConfiguration(string.IsNullOrWhiteSpace(uri) ? null : new Uri(uri), libraryVersion,
                cacheBuster);
            return kernel;
        }

    }
}