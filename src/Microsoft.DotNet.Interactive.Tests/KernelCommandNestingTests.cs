// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests;

public partial class KernelCommandNestingTests : LanguageKernelTestBase
{
    public KernelCommandNestingTests(ITestOutputHelper output) : base(output)
    {
    }
}