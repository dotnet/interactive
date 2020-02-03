// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    internal class KernelCommandPipeline
    {
        private readonly KernelBase _kernel;

        private readonly List<(KernelCommandPipelineMiddleware func, string name)> _middlewares = new List<(KernelCommandPipelineMiddleware func, string name)>();

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
            var invocations = new List<(KernelCommandPipelineMiddleware func, string name)>(_middlewares);

            invocations.Add(
                (
                    func: async (command, context, _) => await _kernel.HandleInternalAsync(command, context),
                    name: nameof(KernelBase.HandleAsync) + $"({_kernel.Name})"
                ));

            var combined =
                invocations
                    .Aggregate(
                        (first, second) =>
                        {
                            return (Combine, first.name + "->" + second.name);

                            async Task Combine(IKernelCommand cmd1, KernelInvocationContext ctx1, KernelPipelineContinuation next)
                            {
                                await first.func(cmd1, ctx1, async (cmd2, ctx2) =>
                                {
                                    Debug.WriteLine($"{first.name}: {cmd1}");

                                    await second.func(cmd2, ctx2, next);
                                });
                            }
                        })
                    .func;

            return combined;
        }

        public void AddMiddleware(
            KernelCommandPipelineMiddleware middleware,
            string caller)
        {
            _middlewares.Add((middleware, caller));
            _pipeline = null;
        }
    }
}