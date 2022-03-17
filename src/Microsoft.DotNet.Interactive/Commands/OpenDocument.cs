// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class OpenDocument : KernelCommand
    {
        public string Path { get; }
        public string RegionName { get; }

        public OpenDocument(string path, string regionName = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentOutOfRangeException(nameof(path));
            }

            Path = path;
            RegionName = regionName;
        }
    }
}
