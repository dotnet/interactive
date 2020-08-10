// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public sealed class FactSkipLinux : FactAttribute
    {
        public FactSkipLinux()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Skip = "Ignored on Linux";
            }
        }
    }
}
