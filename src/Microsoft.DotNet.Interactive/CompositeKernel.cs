// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive;

public sealed class CompositeKernel :
    Kernel,
    IEnumerable<Kernel>
{
    private readonly KernelCollection _childKernels;
    private string _defaultKernelName;
    private KernelActionDirective _rootConnectDirective;
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

        if (kernel.KernelSpecifierDirective is { } kernelSpecifierDirective)
        {
            AddDirective(kernelSpecifierDirective);

            if (aliases is not null)
            {
                foreach (var alias in aliases)
                {
                    var aliasDirective = new KernelSpecifierDirective($"#!{alias}", kernel.Name);

                    aliasDirective.TryGetKernelCommandAsync = kernelSpecifierDirective.TryGetKernelCommandAsync;

                    AddDirective(aliasDirective);

                    kernel.KernelInfo.NameAndAliases.Add(alias);
                }
            }
        }

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

    private void AddDirective(KernelSpecifierDirective directive)
    {
        if (KernelInfo.SupportedDirectives.Any(d => d.Name == directive.Name))
        {
            throw new ArgumentException($"The kernel name or alias '{directive.Name}' is already in use.");
        }

        KernelInfo.SupportedDirectives.Add(directive);

        SubmissionParser.ResetParser();
    }

    public void SetDefaultTargetKernelNameForCommand(
        Type commandType,
        string kernelName)
    {
        _defaultKernelNamesByCommandType[commandType] = kernelName;
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

        foreach (var childKernel in ChildKernels)
        {
            if (childKernel.SupportsCommand(command))
            {
                var childCommand = new RequestKernelInfo(childKernel.Name);
                childCommand.SetParent(command, true);
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
            directiveNode is { Kind : DirectiveNodeKind.Action } actionDirectiveNode)
        {
            // if the first token has been specified, we can narrow down to the specific directive parser that defines this directive

            var directiveName = directiveNode.ChildNodesAndTokens[0].Text;

            var kernel = this.FindKernelByName(actionDirectiveNode.TargetKernelName) ?? this;
            
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

    public void AddKernelConnector<T>(ConnectKernelDirective<T> connectDirective)
        where T : ConnectKernelCommand
    {
        if (_rootConnectDirective is null)
        {
            _rootConnectDirective = new("#!connect")
            {
                Description = "Connects additional subkernels"
            };

            KernelInfo.SupportedDirectives.Add(_rootConnectDirective);
        }

        _rootConnectDirective.Subcommands.Add(connectDirective);

        // FIX: (AddKernelConnector) don't add it to the root
        AddDirective<T>(connectDirective,
                     async (command, context) =>
                     {
                         await ConnectKernel(
                             command,
                             connectDirective,
                             context);
                     });

        SubmissionParser.ResetParser();
    }

    private async Task ConnectKernel<TCommand>(
        TCommand command,
        ConnectKernelDirective<TCommand> connectDirective,
        KernelInvocationContext context) 
        where TCommand : ConnectKernelCommand
    {
        var connectedKernels = await connectDirective.ConnectKernelsAsync(
                                   command,
                                   context);

        foreach (var connectedKernel in connectedKernels)
        {
            Add(connectedKernel);

            var chooseKernelDirective =
                KernelInfo.SupportedDirectives.OfType<KernelSpecifierDirective>()
                          .Single(d => d.KernelName == connectedKernel.Name);

            if (!string.IsNullOrWhiteSpace(connectDirective.ConnectedKernelDescription))
            {
                chooseKernelDirective.Description = connectDirective.ConnectedKernelDescription;
            }

            chooseKernelDirective.Description += " (Connected kernel)";

            context.Display($"Kernel added: #!{connectedKernel.Name}");
        }
    }

    public KernelHost Host => _host;

    internal void SetHost(KernelHost host)
    {
        if (_host is not null)
        {
            throw new InvalidOperationException("Host cannot be changed");
        }

        _host = host;

        KernelInfo.Uri = _host.Uri;

        _childKernels.NotifyThatHostWasSet();
    }
}