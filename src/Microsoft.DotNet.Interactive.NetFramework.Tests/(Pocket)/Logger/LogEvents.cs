// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Events;

namespace Pocket;

internal partial class LogEvents
{
    public static IDisposable SubscribeToPocketLogger(this TestContext output) =>
        Subscribe(
            e => output.WriteLine(e.ToLogString()),
            new[]
            {
                typeof(LogEvents).Assembly,
                typeof(KernelEvent).Assembly
            });
}