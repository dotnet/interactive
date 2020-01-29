// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    public class KernelCommandPipeline
    {
        private readonly KernelBase _kernel;

        private readonly List<KernelCommandPipelineMiddleware> _middlewares = new List<KernelCommandPipelineMiddleware>();

        private KernelCommandPipelineMiddleware _pipeline;

        public KernelCommandPipeline(KernelBase kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        private void EnsureMiddlewarePipelineIsInitialized()
        {
            if (_pipeline == null)
            {
                _pipeline = BuildPipeline();
            }
        }

        internal async Task SendAsync(
            IKernelCommand command,
            KernelInvocationContext context)
        {
            EnsureMiddlewarePipelineIsInitialized();

            try
            {
                await _pipeline(command, context, (_, __) => Task.CompletedTask);
            }
            catch (Exception exception)
            {
                context.Fail(exception);
            }
        }

        private KernelCommandPipelineMiddleware BuildPipeline()
        {
            var invocations = new List<KernelCommandPipelineMiddleware>(_middlewares);

            invocations.Add(async (command, context, _) =>
            {
                await _kernel.HandleInternalAsync(command, context);
            });

            return invocations.Aggregate(
                (function, continuation) =>
                    (cmd1, ctx1, next) =>
                        function(cmd1, ctx1, (cmd2, ctx2) =>
                                     continuation(cmd2, ctx2, next)));
        }

        public void AddMiddleware(KernelCommandPipelineMiddleware middleware)
        {
            _middlewares.Add(middleware);
            _pipeline = null;
        }
    }
}