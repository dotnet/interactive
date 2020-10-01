// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public static class DataExplorerSettingsExtensions
    {
        public static DataExplorerSettings UseUri(this DataExplorerSettings settings, string uri, string context = null, string cacheBuster = null) => UseUri(settings,new Uri(uri), context, cacheBuster);
        
        public static DataExplorerSettings UseUri(this DataExplorerSettings settings, Uri uri,string context = null, string cacheBuster = null)
        {
            settings.Uri = uri;
            settings.Context = context;
            settings.CacheBuster = cacheBuster;
            return settings;
        }

    }
}