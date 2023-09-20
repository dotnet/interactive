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
using System.Linq;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive;

public sealed class CompositeKernel :
    Kernel,
    IEnumerable<Kernel>
{
    private readonly KernelCollection _childKernels;
    private string _defaultKernelName;
    private Command _connectDirective;
    private KernelHost _host;
    private readonly ConcurrentDictionary<Type, string> _defaultKernelNamesByCommandType = new();

    public CompositeKernel(string name = null) : base(name ?? ".NET")
    {
        KernelInfo.IsComposite = true;
        _childKernels = new(this);
    }

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

        kernel.SetScheduler(Scheduler);

        if (aliases is not null)
        {
            kernel.KernelInfo.NameAndAliases.UnionWith(aliases);
        }

        AddChooseKernelDirective(kernel);

        _childKernels.Add(kernel);

        RegisterForDisposal(kernel.KernelEvents.Subscribe(PublishEvent));
        RegisterForDisposal(kernel);

        if (KernelInvocationContext.Current is { } current)
        {
            var kernelInfoProduced = new KernelInfoProduced(kernel.KernelInfo, current.Command);
            current.Publish(kernelInfoProduced);
        }
        else
        {
            var kernelInfoProduced = new KernelInfoProduced(kernel.KernelInfo, KernelCommand.None);
            PublishEvent(kernelInfoProduced);
        }
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

            if (!_defaultKernelNamesByCommandType.TryGetValue(command.GetType(), out targetKernelName))
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

    private protected override async Task HandleRequestKernelInfoAsync(
        RequestKernelInfo command,
        KernelInvocationContext context)
    {
        context.Publish(new KernelInfoProduced(KernelInfo, command));

        command.ShouldResultIncludeEventsFromChildren = true;

        foreach (var childKernel in ChildKernels)
        {
            if (childKernel.SupportsCommand(command))
            {
                var childCommand = new RequestKernelInfo(childKernel.Name);
                childCommand.SetParent(command);
                childCommand.RoutingSlip.ContinueWith(command.RoutingSlip);
                await childKernel.HandleAsync(childCommand, context);
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

            var kernel = this.FindKernelByName(actionDirectiveNode.ParentKernelName) ?? this;
            
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
                var connectedKernels = await connectionCommand.ConnectKernelsAsync(context, commandLineContext);
                foreach (var connectedKernel in connectedKernels)
                {

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
                }
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