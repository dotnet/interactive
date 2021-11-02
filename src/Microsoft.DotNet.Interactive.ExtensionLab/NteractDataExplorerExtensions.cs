// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public static class NteractDataExplorerExtensions
    {
        public static T UseNteractDataExplorer<T>(this T kernel, Uri libraryUri = null, string libraryVersion = null, string cacheBuster = null) where T : Kernel
        {
            NteractDataExplorer.RegisterFormatters();
            NteractDataExplorer.SetDefaultConfiguration(libraryUri, libraryVersion, cacheBuster);
            DataExplorer.Register<TabularDataResource, NteractDataExplorer>();
            return kernel;
        }
    }
}