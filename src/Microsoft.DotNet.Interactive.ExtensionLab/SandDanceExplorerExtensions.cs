// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public static class SandDanceExplorerExtensions
    {
        public static T UseSandDanceExplorer<T>(this T kernel, Uri libraryUri = null, string libraryVersion = null, string cacheBuster = null) where T : Kernel
        {
            SandDanceDataExplorer.RegisterFormatters();
            SandDanceDataExplorer.ConfigureDefaults(libraryUri, libraryVersion,
                cacheBuster);

            DataExplorer.Register<TabularDataResource, SandDanceDataExplorer>();
            return kernel;
        }

    }
}