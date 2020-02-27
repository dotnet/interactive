// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.DotNet.Interactive.App.SignalR
{
    public class KernelHub : Hub
    {
        private readonly IKernel _kernel;

        public KernelHub(IKernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

    }
}