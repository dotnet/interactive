// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public sealed class WindowsFact : FactAttribute
{
    public WindowsFact()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Skip = "Only supported on Windows";
        }
    }
}