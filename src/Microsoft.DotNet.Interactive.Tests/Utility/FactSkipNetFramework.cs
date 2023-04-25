// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public sealed class FactSkipNetFramework : FactAttribute
{
    public FactSkipNetFramework(string reason = null)
    {
        if (RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase))
        {
            Skip = string.IsNullOrWhiteSpace(reason) ? "Ignored on .NET Framework" : reason;
        }
    }
}