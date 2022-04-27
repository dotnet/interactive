// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Extensions;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive
{
    public sealed class CompositeKernel :
        Kernel,
        IExtensibleKernel,
        IEnumerable<Kernel>
    {
        private readonly ConcurrentQueue<PackageAdded> _packagesToCheckForExtensions = new();
        private readonly KernelCollection _childKernels;
        private readonly AssemblyBasedExtensionLoader _extensionLoader = new();
        private readonly ScriptBasedExtensionLoader _scriptExtensionLoader = new();
        private string _defaultKernelName;
        private Command _connectDirective;
        private KernelHost _host;
        private readonly ConcurrentDictionary<Type, string> _defaultKernelNamesByCommandType = new();

        public CompositeKernel(string name = null) : base(name ?? ".NET")
        {
            _childKernels = new(this);

            ListenForPackagesToScanForExtensions();
        }

        private void ListenForPackagesToScanForExtensions() =>
            RegisterForDisposal(KernelEvents
                                .OfType<PackageAdded>()
                                .Where(pa => pa?.PackageReference.PackageRoot is not null)
                                .Distinct(pa => pa.PackageReference.PackageRoot)
                                .Subscribe(added => _packagesToCheckForExtensions.Enqueue(added)));

        public string DefaultKernelName
        {
            get => _defaultKernelName ??
                   (ChildKernels.Count == 1
                        ? ChildKernels.Single().Name
                        : null);
            set => _defaultKernelName = value;
        }

        public void Add(Kernel kernel, IEnumerable<string> aliases = null)
        {
            if (kernel is null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (kernel.ParentKernel is not null)
            {
                throw new InvalidOperationException($"Kernel \"{kernel.Name}\" already has a parent: \"{kernel.ParentKernel.Name}\".");
            }

            if (kernel is CompositeKernel)
            {
                throw new ArgumentException($"{nameof(CompositeKernel)} cannot be added as a child kernel.", nameof(kernel));
            }

            kernel.ParentKernel = this;
            kernel.RootKernel = RootKernel;

            kernel.AddMiddleware(LoadExtensions);
            kernel.SetScheduler(Scheduler);

            if (aliases is not null)
            {
                kernel.KernelInfo.NameAndAliases.UnionWith(aliases);
            }

            AddChooseKernelDirective(kernel);

            _childKernels.Add(kernel);

            RegisterForDisposal(kernel.KernelEvents.Subscribe(PublishEvent));
            RegisterForDisposal(kernel);
        }

        public void SetDefaultTargetKernelNameForCommand(
            Type commandType,
            string kernelName)
        {
            _defaultKernelNamesByCommandType[commandType] = kernelName;
        }

        private void AddChooseKernelDirective(Kernel kernel)
        {
            var chooseKernelCommand = kernel.ChooseKernelDirective;

            foreach (var alias in kernel.KernelInfo.Aliases)
            {
                chooseKernelCommand.AddAlias($"#!{alias}");
            }

            AddDirective(chooseKernelCommand);
        }

        private async Task LoadExtensions(
            KernelCommand command,
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
                         packageRootDir,
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

        public KernelCollection ChildKernels => _childKernels;

        protected override void SetHandlingKernel(KernelCommand command, KernelInvocationContext context)
        {
            context.HandlingKernel = GetHandlingKernel(command, context);
        }

        private protected override Kernel GetHandlingKernel(
            KernelCommand command,
            KernelInvocationContext context)
        {
            Kernel kernel;

            if (command.DestinationUri is not null)
            {
                if (_childKernels.TryGetByUri(command.DestinationUri, out kernel))
                {
                    return kernel;
                }
            }

            var targetKernelName = command.TargetKernelName;

            if (targetKernelName is null)
            {
                if (CanHandle(command))
                {
                    return this;
                }
                else if (_defaultKernelNamesByCommandType.TryGetValue(command.GetType(), out targetKernelName))
                {

                }
                else 
                {

                    targetKernelName = DefaultKernelName;
                }
            }

            if (targetKernelName is not null)
            {
                if (_childKernels.TryGetByAlias(targetKernelName, out kernel))
                {
                    return kernel;
                }
            }

            kernel = _childKernels.Count switch
            {
                0 => null,
                1 => _childKernels.Single(),
                _ => context?.HandlingKernel
            };

            if (kernel is null)
            {
                return this;
            }

            return kernel;
        }

        internal override async Task HandleAsync(
            KernelCommand command,
            KernelInvocationContext context)
        {
            if (!string.IsNullOrWhiteSpace(command.TargetKernelName) &&
                _childKernels.TryGetByAlias(command.TargetKernelName, out var kernel))
            {
                // route to a subkernel
                await kernel.Pipeline.SendAsync(command, context);
            }
            else
            {
                await base.HandleAsync(command, context);
            }
        }

        public override async Task HandleAsync(
            RequestKernelInfo command,
            KernelInvocationContext context)
        {
            foreach (var childKernel in ChildKernels)
            {
                if (childKernel.SupportsCommand(command))
                {
                    await childKernel.HandleAsync(command, context);
                }
            }
        }

        private protected override IEnumerable<Parser> GetDirectiveParsersForCompletion(
            DirectiveNode directiveNode,
            int requestPosition)
        {
            var upToCursor =
                directiveNode.Text[..requestPosition];

            var indexOfPreviousSpace =
                upToCursor.LastIndexOf(" ", StringComparison.CurrentCultureIgnoreCase);

            var compositeKernelDirectiveParser = SubmissionParser.GetDirectiveParser();

            if (indexOfPreviousSpace >= 0 &&
                directiveNode is ActionDirectiveNode actionDirectiveNode)
            {
                // if the first token has been specified, we can narrow down to the specific directive parser that defines this directive

                var directiveName = directiveNode.ChildNodesAndTokens[0].Text;

                var kernel = this.FindKernel(actionDirectiveNode.ParentKernelName);

                if (kernel is null)
                {
                    yield break;
                }

                var languageKernelDirectiveParser = kernel.SubmissionParser.GetDirectiveParser();

                if (IsDirectiveDefinedIn(languageKernelDirectiveParser))
                {
                    // the directive is defined in the subkernel, so this is the only directive parser we need
                    yield return languageKernelDirectiveParser;
                }
                else if (IsDirectiveDefinedIn(compositeKernelDirectiveParser))
                {
                    yield return compositeKernelDirectiveParser;
                }

                bool IsDirectiveDefinedIn(Parser parser) =>
                    parser.Configuration.RootCommand.Children.GetByAlias(directiveName) is { };
            }
            else
            {
                // otherwise, return all directive parsers from the CompositeKernel as well as subkernels
                yield return compositeKernelDirectiveParser;

                foreach (var kernel in ChildKernels)
                {
                    yield return kernel.SubmissionParser.GetDirectiveParser();
                }
            }
        }

        public IEnumerator<Kernel> GetEnumerator() => _childKernels.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public async Task LoadExtensionsFromDirectoryAsync(
            DirectoryInfo directory,
            KernelInvocationContext context)
        {
            await _extensionLoader.LoadFromDirectoryAsync(
                directory,
                this,
                context);

            await _scriptExtensionLoader.LoadFromDirectoryAsync(
                directory,
                this,
                context);
        }

        public void AddKernelConnector(ConnectKernelCommand connectionCommand)
        {
            if (_connectDirective is null)
            {
                _connectDirective = new Command(
                    "#!connect",
                    "Connects additional subkernels");

                AddDirective(_connectDirective);
            }

            connectionCommand.Handler = CommandHandler.Create<KernelInvocationContext, InvocationContext>(
                async (context, commandLineContext) =>
                {
                    var connectedKernel = await connectionCommand.ConnectKernelAsync(context, commandLineContext);

                    Add(connectedKernel);

                    // todo : here the connector should be used to patch the kernelInfo with the right destination uri for the proxy

                    var chooseKernelDirective =
                        Directives.OfType<ChooseKernelDirective>()
                                  .Single(d => d.Kernel == connectedKernel);

                    if (!string.IsNullOrWhiteSpace(connectionCommand.ConnectedKernelDescription))
                    {
                        chooseKernelDirective.Description = connectionCommand.ConnectedKernelDescription;
                    }

                    chooseKernelDirective.Description += " (Connected kernel)";

                    context.Display($"Kernel added: #!{connectedKernel.Name}");
                });

            _connectDirective.Add(connectionCommand);

            SubmissionParser.ResetParser();
        }

        public KernelHost Host => _host;

        internal void SetHost(KernelHost host)
        {
            if (_host is { })
            {
                throw new InvalidOperationException("Host cannot be changed");
            }

            _host = host;

            KernelInfo.Uri = _host.Uri;

            _childKernels.NotifyThatHostWasSet();
        }
    }
}
