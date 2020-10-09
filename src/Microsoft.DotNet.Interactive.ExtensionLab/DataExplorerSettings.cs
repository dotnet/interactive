// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class DataExplorerSettings
    {
        internal DataExplorerSettings()
        {
            RestoreDefault();
        }

        public DataExplorerSettings RestoreDefault()
        {
            Uri = null;
            Context = null;
            CacheBuster = null;
            return this;
        }

        public string Context { get; internal set; }
        public string CacheBuster { get; internal set; }


        public Uri Uri { get; internal  set; }
    }
}