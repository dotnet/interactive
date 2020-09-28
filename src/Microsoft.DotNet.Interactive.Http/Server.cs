// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Http
{
    public class Server : IDisposable
    {
        private readonly Kernel _kernel;

        public Server(Kernel kernel)
        {
            _kernel = kernel;
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
