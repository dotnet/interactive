// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Extensions;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive
{
    public class CompositeKernel : 
        KernelBase,
        IExtensibleKernel,
        IEnumerable<IKernel>
    {
        private readonly ConcurrentQueue<PackageAdded> _packagesToCheckForExtensions = new ConcurrentQueue<PackageAdded>();
        private readonly List<IKernel> _childKernels = new List<IKernel>();
        private readonly Dictionary<string, IKernel> _kernelsByNameOrAlias;
        private readonly AssemblyBasedExtensionLoader _extensionLoader = new AssemblyBasedExtensionLoader();
        private string _defaultKernelName;

        public CompositeKernel() : base(".NET")
        {
            ListenForPackagesToScanForExtensions();

            _kernelsByNameOrAlias = new Dictionary<string, IKernel>();
            _kernelsByNameOrAlias.Add(Name, this);
        }

        private void ListenForPackagesToScanForExtensions() =>
            RegisterForDisposal(KernelEvents
                                .OfType<PackageAdded>()
                                .Where(pa => pa?.PackageReference.PackageRoot != null)
                                .Distinct(pa => pa.PackageReference.PackageRoot)
                                .Subscribe(added => _packagesToCheckForExtensions.Enqueue(added)));

        public string DefaultKernelName
        {
            get => _defaultKernelName;
            set
            {
                _defaultKernelName = value;
                SubmissionParser.KernelLanguage = value;
            }
        }

        public void Add(IKernel kernel, IReadOnlyCollection<string> aliases = null)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (kernel is KernelBase kernelBase)
            {
                if (kernelBase.ParentKernel != null)
                {
                    throw new InvalidOperationException($"Kernel \"{kernelBase.Name}\" already has a parent: \"{kernelBase.ParentKernel.Name}\".");
                }

                kernelBase.ParentKernel = this;
                kernelBase.AddMiddleware(LoadExtensions);
            }

            AddChooseKernelDirective(kernel, aliases);

            _childKernels.Add(kernel);

            _kernelsByNameOrAlias.Add(kernel.Name, kernel);
            if (aliases is {})
            {
                foreach (var alias in aliases)
                {
                    _kernelsByNameOrAlias.Add(alias, kernel);
                }
            }

            if (_childKernels.Count == 1)
            {
                DefaultKernelName = kernel.Name;
            }

            RegisterForDisposal(kernel.KernelEvents.Subscribe(PublishEvent));
            RegisterForDisposal(kernel);
        }

        private void AddChooseKernelDirective(
            IKernel kernel, 
            IEnumerable<string> aliases)
        {
            var chooseKernelCommand = new ChooseKernelDirective(kernel);

            if (aliases is { })
            {
                foreach (var alias in aliases)
                {
                    chooseKernelCommand.AddAlias($"#!{alias}");
                }
            }

            AddDirective(chooseKernelCommand);
        }

        private async Task LoadExtensions(
            IKernelCommand command,
            KernelInvocationContext context,
            KernelPipelineContinuation next)
        {
            await next(command, context);

            while (_packagesToCheckForExtensions.TryDequeue(out var packageAdded))
            {
                var packageRootDir = packageAdded.PackageReference.PackageRoot;

                var extensionDir =
                    new DirectoryInfo
                    (Path.Combine(
                         packageRootDir.FullName,
                         "interactive-extensions",
                         "dotnet"));
                
                if (extensionDir.Exists)
                {
                    await LoadExtensionsFromDirectoryAsync(
                        extensionDir,
                        context);
                }
            }
        }

        public IReadOnlyList<IKernel> ChildKernels => _childKernels;

        protected override void SetHandlingKernel(IKernelCommand command, KernelInvocationContext context)
        {
            var kernel = GetHandlingKernel(command, context);

            context.HandlingKernel = kernel;
        }

        private IKernel GetHandlingKernel(
            IKernelCommand command,
            KernelInvocationContext context)
        {
            var targetKernelName = command switch
            {
                KernelCommandBase kcb => kcb.TargetKernelName ?? DefaultKernelName,
                _ => DefaultKernelName
            };

            IKernel kernel;

            if (targetKernelName != null)
            {
                _kernelsByNameOrAlias.TryGetValue(targetKernelName, out kernel);
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

            return kernel ?? this;
        }

        internal override async Task HandleAsync(
            IKernelCommand command,
            KernelInvocationContext context)
        {

            var kernel = context.HandlingKernel;

            if (kernel is KernelBase kernelBase)
            {
                await kernelBase.RunDeferredCommandsAsync();

                if (kernelBase != this)
                {
                    // route to a subkernel
                    await kernelBase.Pipeline.SendAsync(command, context);
                }
                else
                {
                    await base.HandleAsync(command, context);
                }

                return;
            }

            throw new NoSuitableKernelException(command);
        }

        private protected override IReadOnlyList<CompletionItem> GetDirectiveCompletionItems(
            DirectiveNode directiveNode,
            int requestPosition)
        {
            var directiveParsers = new List<Parser>
            {
                SubmissionParser.GetDirectiveParser()
            };

            for (var i = 0; i < ChildKernels.Count; i++)
            {
                var kernel = ChildKernels[i];

                if (kernel is KernelBase kb)
                {
                    directiveParsers.Add(kb.SubmissionParser.GetDirectiveParser());
                }
            }

            var allCompletions = new List<CompletionItem>();

            foreach (var parser in directiveParsers)
            {
                var parseResult = parser.Parse(directiveNode.Text);

                var completions = parseResult
                                  .GetSuggestions(requestPosition)
                                  .Select(s => SubmissionParser.CompletionItemFor(s, parseResult))
                                  .ToArray();

                allCompletions.AddRange(completions);
            }

            return allCompletions;
        }

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