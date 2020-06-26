// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.DotNet.Interactive.Server
{
    public static class StdIOTransport
    {
        public static  KernelServer CreateServer(Kernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            var input = new TextReaderInputStream(Console.In);
            var output = new TextWriterOutputStream(Console.Out);
            var kernelServer = new KernelServer(kernel, input, output);

            kernel.RegisterForDisposal(kernelServer);
            return kernelServer;
        }

        public static KernelClient CreateClient(Kernel kernel, Process remote)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (remote == null)
            {
                throw new ArgumentNullException(nameof(remote));
            }

            if (!remote.StartInfo.RedirectStandardInput || !remote.StartInfo.RedirectStandardOutput)
            {
                throw new InvalidOperationException("StandardInput and StandardOutput must be redirected");
            }

            var input = new TextReaderInputStream(remote.StandardOutput);
            var output = new TextWriterOutputStream(remote.StandardInput);

            var kernelClient = new KernelClient(input, output);
            kernel.RegisterForDisposal(kernelClient);
            return kernelClient;
        }
    }
}