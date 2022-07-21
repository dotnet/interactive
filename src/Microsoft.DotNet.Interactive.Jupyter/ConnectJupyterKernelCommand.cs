// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.KernelProxy;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class ConnectJupyterKernelCommand : ConnectKernelCommand
    {
        public ConnectJupyterKernelCommand() : base("jupyter",
                                            "Connects to a installed jupyter kernel")
        {
            AddOption(KernelType);
            AddOption(TargetUrl);
            AddOption(Token);
        }

        public Option<string> KernelType { get; } =
        new("--kernel-type", "The kernel spec to connect to")
        {
            IsRequired = true
        };

        public Option<string> TargetUrl { get; } =
        new("--url", "URl to connect to the jupyter server")
        {
        };

        public Option<string> Token { get; } =
        new("--token", "token to connect to the jupyter server")
        { 
        };

        public override Task<Kernel> ConnectKernelAsync(
            KernelInvocationContext context,
            InvocationContext commandLineContext)
        {
            var kernelType = commandLineContext.ParseResult.GetValueForOption(KernelType);
            var targetUrl = commandLineContext.ParseResult.GetValueForOption(TargetUrl);
            var token = commandLineContext.ParseResult.GetValueForOption(Token);

            JupyterKernelConnector connector = null;
            if (targetUrl is not null)
            {
                var connection = new JupyterApiConnection(new Uri(targetUrl), token);
                connector = new JupyterKernelConnector(connection, connection, connection, kernelType);
            }
            else
            {
                var connectionInfo = ConnectionInformation.Load(new System.IO.FileInfo($"\\{kernelType}\\kernel.json"));
                var connection = new JupyterZMQConnection(connectionInfo);

            }

            var localName = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);

            return connector?.CreateKernelAsync(localName);
        }
    }
}
