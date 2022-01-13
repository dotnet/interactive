// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
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

using DocumentKernelName = Microsoft.DotNet.Interactive.Documents.KernelName;

namespace Microsoft.DotNet.Interactive
{
    public sealed class CompositeKernel :
        Kernel,
        IExtensibleKernel,
        IEnumerable<Kernel>
    {
        private readonly ConcurrentQueue<PackageAdded> _packagesToCheckForExtensions = new();
        private readonly List<Kernel> _childKernels = new();
        private readonly Dictionary<string, Kernel> _kernelsByNameOrAlias;
        private readonly AssemblyBasedExtensionLoader _extensionLoader = new();
        private readonly ScriptBasedExtensionLoader _scriptExtensionLoader = new();
        private string _defaultKernelName;
        private Command _connectDirective;
        private KernelHost _host;
        private readonly Dictionary<Kernel, HashSet<string>> _kernelToNameOrAlias;

        public CompositeKernel() : base(".NET")
        {
            ListenForPackagesToScanForExtensions();

            _kernelsByNameOrAlias = new Dictionary<string, Kernel>
            {
                [Name] = this
            };

            _kernelToNameOrAlias = new Dictionary<Kernel, HashSet<string>>
            {
                [this] = new() { Name }
            };
        }

        private void ListenForPackagesToScanForExtensions() =>
            RegisterForDisposal(KernelEvents
                                .OfType<PackageAdded>()
                                .Where(pa => pa?.PackageReference.PackageRoot is not null)
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

        public void Add(Kernel kernel, IReadOnlyCollection<string> aliases = null)
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

            if ((aliases ?? Array.Empty<string>()).Append(kernel.Name).FirstOrDefault(a => _kernelsByNameOrAlias.ContainsKey(a)) is { } collidingAlias)
            {
                throw new ArgumentException($"Alias '#!{collidingAlias}' is already in use.");
            }


            kernel.ParentKernel = this;
            kernel.RootKernel = RootKernel;

            kernel.AddMiddleware(LoadExtensions);
            kernel.SetScheduler(Scheduler);

            AddChooseKernelDirective(kernel, aliases);

            _childKernels.Add(kernel);
            
            Host?.AddKernelInfo(kernel, new KernelInfo(kernel.Name, aliases));

            _kernelToNameOrAlias.Add(kernel, new HashSet<string>{kernel.Name});

            _kernelsByNameOrAlias.Add(kernel.Name, kernel);

            if (aliases is { })
            {
                foreach (var alias in aliases)
                {
                    _kernelsByNameOrAlias.Add(alias, kernel);
                    _kernelToNameOrAlias[kernel].Add(alias);
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
            Kernel kernel,
            IEnumerable<string> aliases = null)
        {
            var chooseKernelCommand = kernel.ChooseKernelDirective;

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

        public IReadOnlyList<Kernel> ChildKernels => _childKernels;

        protected override void SetHandlingKernel(KernelCommand command, KernelInvocationContext context)
        {
            context.HandlingKernel = GetHandlingKernel(command, context);
        }

        private Kernel GetHandlingKernel(
            KernelCommand command,
            KernelInvocationContext context)
        {
            var targetKernelName = command switch
            {
                { } _ => GetKernelNameFromCommand() ?? DefaultKernelName,
                _ => DefaultKernelName
            };

            Kernel kernel;

            if (targetKernelName is not null)
            {
                _kernelsByNameOrAlias.TryGetValue(targetKernelName, out kernel);
            }
            else
            {
                kernel = _childKernels.Count switch
                {
                    0 => this,
                    1 => _childKernels[0],
                    _ => context?.HandlingKernel
                };
            }

            return kernel ?? this;

            string GetKernelNameFromCommand()
            {
                return command.TargetKernelName;
            }
        }

        private protected override SchedulingScope GetHandlingKernelCommandScope(
            KernelCommand command, KernelInvocationContext context)
        {
            return GetHandlingKernel(command, context).SchedulingScope;
        }

        private protected override string GetHandlingKernelName(
            KernelCommand command, KernelInvocationContext context)
        {
            return GetHandlingKernel(command, context).Name;
        }

        internal override async Task HandleAsync(
            KernelCommand command,
            KernelInvocationContext context)
        {
            if (!string.IsNullOrWhiteSpace(command.TargetKernelName) 
                && !_kernelToNameOrAlias[this].Contains(command.TargetKernelName) 
                && _kernelsByNameOrAlias.TryGetValue(command.TargetKernelName, out var kernel))
            {
                // route to a subkernel
               
                await kernel.Pipeline.SendAsync(command, context);
            }
            else
            {
                await base.HandleAsync(command, context);
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

                for (var i = 0; i < ChildKernels.Count; i++)
                {
                    var kernel = ChildKernels[i];

                    if (kernel is { })
                    {
                        yield return kernel.SubmissionParser.GetDirectiveParser();
                    }
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

        public void AddKernelConnection<TOptions>(
            ConnectKernelCommand<TOptions> connectionCommand)
            where TOptions : IKernelConnector
        {
            var kernelNameOption = new Option<string>(
                "--kernel-name",
                "The name of the subkernel to be added");

            if (_connectDirective is null)
            {
                _connectDirective = new Command(
                    "#!connect",
                    "Connects additional subkernels");

                _connectDirective.Add(kernelNameOption);

                AddDirective(_connectDirective);
            }

            connectionCommand.Handler = CommandHandler.Create<
                string, TOptions, KernelInvocationContext>(
                async (kernelName, options, context) =>
                {
                    var connectedKernel = await connectionCommand.ConnectKernelAsync(new KernelInfo(kernelName), options, context);
                 
                    Add(connectedKernel);

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

            connectionCommand.Add(kernelNameOption);
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

            var kernelsToRegister = _kernelsByNameOrAlias
                                    .GroupBy(e => e.Value)
                                    .Select(g =>
                                    {
                                        var localName = g.Key.Name;
                                        var aliases = new HashSet<string>(g.Select(v => v.Key));
                                        aliases.Remove(localName);

                                        return (g.Key, new KernelInfo(localName, aliases.ToArray()));
                                    });

            foreach (var (kernel, kernelInfo) in kernelsToRegister)
            {
                _host.AddKernelInfo(kernel, kernelInfo);
            }
        }
    }
}
