// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class NteractDataExplorer
    {
        public NteractDataExplorer(TabularDataResource source)
        {
            Id = Guid.NewGuid().ToString("N");
            TabularDataResource = source;
        }

        public string Id { get; }

        public TabularDataResource TabularDataResource { get; }
    }
}