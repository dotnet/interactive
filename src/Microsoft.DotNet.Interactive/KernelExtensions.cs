// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Messages;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelExtensions
    {
        public static Kernel FindKernel(this Kernel kernel, string name)
        {
            var root = kernel
                       .RecurseWhileNotNull(k => k switch
                       {
                           { } kb => kb.ParentKernel,
                           _ => null
                       })
                       .LastOrDefault();

            return root switch
            {
                _ when kernel.Name == name => kernel,
                CompositeKernel c =>
                c.Directives
                 .OfType<ChooseKernelDirective>()
                 .Where(d => d.HasAlias($"#!{name}"))
                 .Select(d => d.Kernel)
                 .SingleOrDefault(),
                _ => null
            };
        }

        public static async Task ProcessMessageAsync(
            this Kernel kernel,
            KernelChannelMessage kernelMessage)
        {
            switch (kernelMessage)
            {
                case CommandKernelMessage commandMessage:
                    await kernel.SendAsync(commandMessage.Command);
                    break;

                default:
                    throw new ArgumentException($"Unrecognized message label: {kernelMessage.Label}", nameof(kernelMessage));
            }
        }

        public static Task<KernelCommandResult> SendAsync(
            this Kernel kernel,
            KernelCommand command)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return kernel.SendAsync(command, CancellationToken.None);
        }

        public static Task<KernelCommandResult> SubmitCodeAsync(
            this Kernel kernel,
            string code)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return kernel.SendAsync(new SubmitCode(code), CancellationToken.None);
        }

        public static T UseLog<T>(this T kernel)
            where T : Kernel
        {
            var command = new Command("#!log", "Enables session logging.");

            var logStarted = false;

            command.Handler = CommandHandler.Create<KernelInvocationContext>(context =>
            {
                if (logStarted)
                {
                    return;
                }

                logStarted = true;

                kernel.AddMiddleware(async (kernelCommand, c, next) =>
                {
                    Log(c, kernelCommand.ToLogString());

                    await next(kernelCommand, c);
                });

                var disposable = new CompositeDisposable
                {
                    kernel.KernelEvents.Subscribe(e =>
                    {
                        if (KernelInvocationContext.Current is {} currentContext)
                        {
                            if (e is DiagnosticEvent || 
                                e is DisplayEvent || 
                                e is DiagnosticsProduced)
                            {
                                return;
                            }

                            Log(currentContext, e.ToLogString());
                        }
                    }),
                    LogEvents.Subscribe(e =>
                    {
                        if (KernelInvocationContext.Current is {} currentContext)
                        {
                            Log(currentContext, e.ToLogString());
                        }
                    })
                };

                kernel.RegisterForDisposal(disposable);

                Log(context, "Logging enabled");

                static void Log(KernelInvocationContext c, string message) => c.Publish(new DiagnosticLogEntryProduced(message));
            });

            kernel.AddDirective(command);

            return kernel;
        }

        public static T UseDotNetVariableSharing<T>(this T kernel)
            where T : DotNetKernel
        {
            var variableNameArg = new Argument<string>(
                "name",
                "The name of the variable to create in the destination kernel");

            variableNameArg.AddSuggestions((_,__) =>
            {
                if (kernel.ParentKernel is { } composite)
                {
                    return composite.ChildKernels
                                    .OfType<DotNetKernel>()
                                    .SelectMany(k => k.GetVariableNames());
                }

                return Array.Empty<string>();
            });

            var fromKernelOption = new Option<string>(
                "--from",
                "The name of the kernel where the variable has been previously declared");

            fromKernelOption.AddSuggestions((_,__) =>
            {
                if (kernel.ParentKernel is { } composite)
                {
                    return composite.ChildKernels
                                    .OfType<DotNetKernel>()
                                    .Select(k => k.Name);
                }

                return Array.Empty<string>();
            });

            var share = new Command("#!share", "Share a .NET variable between subkernels")
            {
                fromKernelOption,
                variableNameArg
            };

            share.Handler = CommandHandler.Create<string, string, KernelInvocationContext>(async (from, name, context) =>
            {
                if (kernel.FindKernel(from) is DotNetKernel fromKernel)
                {
                    if (fromKernel.TryGetVariable(name, out object shared))
                    {
                        await kernel.SetVariableAsync(name, shared);
                    }
                }
            });

            kernel.AddDirective(share);

            return kernel;
        }

        public static TKernel UseWho<TKernel>(this TKernel kernel)
            where TKernel : DotNetKernel
        {
            kernel.AddDirective(who_and_whos());
            Formatter.Register(new CurrentVariablesFormatter());
            return kernel;
        }

        private static Command who_and_whos()
        {
            var command = new Command("#!whos", "Display the names of the current top-level variables and their values.")
            {
                Handler = CommandHandler.Create((ParseResult parseResult, KernelInvocationContext context) =>
                {
                    var alias = parseResult.CommandResult.Token.Value;

                    var detailed = alias == "#!whos";

                    Display(context, detailed);

                    return Task.CompletedTask;
                })
            };

            // TODO: (who_and_whos) this should be a separate command with separate help
            command.AddAlias("#!who");

            return command;

            void Display(KernelInvocationContext context, bool detailed)
            {
                if (context.Command is SubmitCode &&
                    context.HandlingKernel is DotNetKernel kernel)
                {
                    var variables = kernel.GetVariableNames()
                                          .Select(name =>
                                          {
                                              kernel.TryGetVariable(name, out object v);
                                              return new CurrentVariable(name, v.GetType(), v);
                                          });

                    var currentVariables = new CurrentVariables(
                        variables,
                        detailed);

                    var html = currentVariables
                        .ToDisplayString(HtmlFormatter.MimeType);

                    context.Publish(
                        new DisplayedValueProduced(
                            html,
                            context.Command,
                            new[]
                            {
                                new FormattedValue(
                                    HtmlFormatter.MimeType,
                                    html)
                            }));
                }
            }
        }

        public static CompositeKernel UseKernelClientConnection<TOptions>(
            this CompositeKernel kernel,
            ConnectKernelCommand<TOptions> command)
            where TOptions : KernelConnectionOptions
        {
            kernel.AddKernelConnection(command);

            return kernel;
        }

        [DebuggerStepThrough]
        public static T LogEventsToPocketLogger<T>(this T kernel)
            where T : Kernel
        {
            var disposables = new CompositeDisposable();

            disposables.Add(
                kernel.KernelEvents
                      .Subscribe(
                          e =>
                          {
                              Logger.Log.Info("{kernel}: {event}",
                                              kernel.Name,
                                              e);
                          }));

            kernel.VisitSubkernels(k =>
            {
                disposables.Add(
                    k.KernelEvents.Subscribe(
                        e =>
                        {
                            Logger.Log.Info("{kernel}: {event}",
                                            k.Name,
                                            e);
                        }));
            });

            kernel.RegisterForDisposal(disposables);

            return kernel;
        }

        public static void VisitSubkernels(
            this Kernel kernel,
            Action<Kernel> onVisit,
            bool recursive = false)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (onVisit == null)
            {
                throw new ArgumentNullException(nameof(onVisit));
            }

            if (kernel is CompositeKernel compositeKernel)
            {
                foreach (var subKernel in compositeKernel.ChildKernels)
                {
                    onVisit(subKernel);

                    if (recursive)
                    {
                        subKernel.VisitSubkernels(onVisit, recursive: true);
                    }
                }
            }
        }

        public static void VisitSubkernelsAndSelf(
            this Kernel kernel,
            Action<Kernel> onVisit,
            bool recursive = false)
        {
            onVisit(kernel);

            VisitSubkernels(kernel, onVisit, recursive);
        }

        public static async Task VisitSubkernelsAsync(
            this Kernel kernel,
            Func<Kernel, Task> onVisit,
            bool recursive = false)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (onVisit == null)
            {
                throw new ArgumentNullException(nameof(onVisit));
            }

            if (kernel is CompositeKernel compositeKernel)
            {
                foreach (var subKernel in compositeKernel.ChildKernels)
                {
                    await onVisit(subKernel);

                    if (recursive)
                    {
                        await subKernel.VisitSubkernelsAsync(onVisit, true);
                    }
                }
            }
        }

        public static async Task VisitSubkernelsAndSelfAsync(
            this Kernel kernel,
            Func<Kernel, Task> onVisit,
            bool recursive = false)
        {
            await onVisit(kernel);

            await VisitSubkernelsAsync(kernel, onVisit, recursive);
        }
    }
}