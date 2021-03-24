// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public static class NteractDataExplorerExtensions
    {
        public static T UseNteractDataExplorer<T>(this T kernel, string uri = null, string context = null, string cacheBuster = null) where T : Kernel
        {
            NteractDataExplorer.RegisterFormatters();
            NteractDataExplorer.SetDefaultConfiguration(string.IsNullOrWhiteSpace(uri) ? null : new Uri(uri), context, cacheBuster);
            return kernel;
        }
    }
}