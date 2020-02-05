// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Extensions;

namespace Microsoft.DotNet.Interactive
{
    public class CompositeKernel : 
        KernelBase, 
        IExtensibleKernel,
        IEnumerable<IKernel>
    {
        private readonly ConcurrentQueue<PackageAdded> _packages = new ConcurrentQueue<PackageAdded>();
        private readonly List<IKernel> _childKernels = new List<IKernel>();
        private readonly AssemblyBasedExtensionLoader _extensionLoader = new AssemblyBasedExtensionLoader();

        public CompositeKernel()
        {
            Name = nameof(CompositeKernel);

            RegisterForDisposal(KernelEvents
                .OfType<PackageAdded>()
                .Where(pa => pa?.PackageReference.PackageRoot != null)
                .Distinct(pa => pa.PackageReference.PackageRoot)
                .Subscribe(_packages.Enqueue));
        }

        public string DefaultKernelName { get; set; }

        public void Add(IKernel kernel, IEnumerable<string> aliases = null)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            _childKernels.Add(kernel);

            if (kernel is KernelBase kernelBase)
            {
                kernelBase.AddMiddleware(LoadExtensions);
            }

            var chooseKernelCommand = new Command($"#!{kernel.Name}")
            {
                Handler = CommandHandler.Create<KernelInvocationContext>(
                    context => { context.HandlingKernel = kernel; })
            };

            if (aliases is { })
            {
                foreach (var alias in aliases)
                {
                    chooseKernelCommand.AddAlias(alias);
                }
            }

            AddDirective(chooseKernelCommand);
            RegisterForDisposal(kernel.KernelEvents.Subscribe(PublishEvent));
            RegisterForDisposal(kernel);
        }

        private async Task LoadExtensions(
            IKernelCommand command, 
            KernelInvocationContext context,
            KernelPipelineContinuation next)
        {
            await next(command, context);

            while (_packages.TryDequeue(out var packageAdded))
            {
                var packageRootDir = packageAdded.PackageReference.PackageRoot;

                await LoadExtensionsFromDirectoryAsync(
                    packageRootDir,
                    context);
            }
        }

        protected override void SetHandlingKernel(IKernelCommand command, KernelInvocationContext context)
        {
            var kernel = GetHandlingKernel(command, context);

            if (command is KernelCommandBase commandBase && 
                commandBase.HandlingKernel == null)
            {
                commandBase.HandlingKernel = kernel;
            }

            if (context.HandlingKernel == null)
            {
                context.HandlingKernel = kernel;
            }
        }

        private IKernel GetHandlingKernel(
            IKernelCommand command,
            KernelInvocationContext context)
        {
            var commandBase = command as KernelCommandBase;

            var targetKernelName = commandBase?.TargetKernelName
                                   ?? DefaultKernelName;

            IKernel kernel;

            if (targetKernelName != null)
            {
                kernel = targetKernelName == Name
                             ? this
                             : ChildKernels.FirstOrDefault(k => k.Name == targetKernelName);
            }
            else
            {
                kernel = _childKernels.Count switch
                {
                    0 => this,
                    1 => _childKernels[0],
                    _ => context.HandlingKernel
                };
            }

            return kernel ?? throw new NoSuitableKernelException(command);
        }

        protected internal override async Task HandleAsync(
            IKernelCommand command,
            KernelInvocationContext context)
        {
            var kernel = context.HandlingKernel;

            if (kernel is KernelBase kernelBase)
            {
                await kernelBase.RunDeferredCommandsAsync();

                if (kernelBase != this)
                {
                    await kernelBase.Pipeline.SendAsync(command, context);
                }
                else
                {
                    await command.InvokeAsync(context);
                }

                return;
            }

            throw new NoSuitableKernelException(command);
        }

        internal override Task HandleInternalAsync(IKernelCommand command, KernelInvocationContext context)
        {
            return HandleAsync(command, context);
        }

        public IReadOnlyCollection<IKernel> ChildKernels => _childKernels;

        public IEnumerator<IKernel> GetEnumerator() => _childKernels.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public async Task LoadExtensionsFromDirectoryAsync(
            DirectoryInfo directory,
            KernelInvocationContext context)
        {
            await _extensionLoader.LoadFromDirectoryAsync(
                directory,
                this,
                context);
        }
    }
}