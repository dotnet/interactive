// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelExtensions
    {
        public static T UseQuitCommand<T>(this T kernel, Func<Task> onQuitAsync = null) where T : Kernel
        {
            kernel.RegisterCommandHandler<Quit>(async (quit, context) =>
            {
                if (onQuitAsync is not null)
                {
                    await onQuitAsync();
                }
                else
                {
                    ShutDown();
                }
            });

            return kernel;

            void ShutDown() 
            {
                Environment.Exit(0);
            }
        }

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

        public static Task<KernelCommandResult> SendAsync(
            this Kernel kernel,
            KernelCommand command)
        {
            if (kernel is null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return kernel.SendAsync(command, CancellationToken.None);
        }

        public static Task<KernelCommandResult> SubmitCodeAsync(
            this Kernel kernel,
            string code)
        {
            if (kernel is null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return kernel.SendAsync(new SubmitCode(code), CancellationToken.None);
        }

        public static T UseLogMagicCommand<T>(this T kernel)
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
                    PublishLogEvent(c, kernelCommand.ToLogString());

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

                            PublishLogEvent(currentContext, e.ToLogString());
                        }
                    }),
                    LogEvents.Subscribe(e =>
                    {
                        if (KernelInvocationContext.Current is {} currentContext)
                        {
                            PublishLogEvent(currentContext, e.ToLogString());
                        }
                    })
                };

                kernel.RegisterForDisposal(disposable);

                PublishLogEvent(context, "Logging enabled");

                static void PublishLogEvent(KernelInvocationContext c, string message) => c.Publish(new DiagnosticLogEntryProduced(message, c.Command));
            });

            kernel.AddDirective(command);

            return kernel;
        }

        public static T UseValueSharing<T>(this T kernel)
            where T : Kernel, ISupportGetValue
        {
            var variableNameArg = new Argument<string>(
                "name",
                "The name of the variable to create in the destination kernel");

            variableNameArg.AddSuggestions((_,__) =>
            {
                if (kernel.ParentKernel is { } composite)
                {
                    return composite.ChildKernels
                                    .OfType<ISupportGetValue>()
                                    .SelectMany(k => k.GetValueInfos().Select(vd => vd.Name)).ToArray();
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
                                    .Where(k => k is ISupportGetValue)
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
                if (kernel.FindKernel(from) is ISupportGetValue fromKernel)
                {
                    if (fromKernel.TryGetValue(name, out object shared))
                    {
                        try
                        {
                            await ((ISupportSetValue)kernel).SetValueAsync(name, shared);
                        } catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Error sharing value '{name}' from kernel '{from}' into kernel '{kernel.Name}'. {ex.Message}", ex);
                        }
                    }
                }
            });

            kernel.AddDirective(share);

            return kernel;
        }

        public static TKernel UseWho<TKernel>(this TKernel kernel)
            where TKernel : Kernel, ISupportGetValue
        {
            kernel.AddDirective(who());
            kernel.AddDirective(whos());
            Formatter.Register(new CurrentVariablesFormatter());
            return kernel;
        }

        private static Command who()
        {
            var command = new Command("#!who", "Display the names of the current top-level variables.")
            {
                Handler = CommandHandler.Create(async (ParseResult parseResult, KernelInvocationContext context) =>
                {
                    await DisplayValues(context, false);
                })
            };

            return command;
        }

        private static Command whos()
        {
            var command = new Command("#!whos", "Display the names of the current top-level variables and their values.")
            {
                Handler = CommandHandler.Create(async (ParseResult parseResult, KernelInvocationContext context) =>
                {
                    await  DisplayValues(context, true);
                })
            };

            return command;
        }

        private static async Task DisplayValues(KernelInvocationContext context, bool detailed)
        {
            if (context.Command is SubmitCode &&
                context.HandlingKernel is ISupportGetValue)
            {
                var nameEvents = new List<ValueInfosProduced>();

                var result = await context.HandlingKernel.SendAsync(new RequestValueInfos(context.Command.TargetKernelName));
                using var _ = result.KernelEvents.OfType<ValueInfosProduced>().Subscribe(e => nameEvents.Add(e));

                var valueNames = nameEvents.SelectMany(e => e.ValueInfos.Select(d => d.Name)).Distinct();

                var valueEvents = new List<ValueProduced>();
                var valueCommands = valueNames.Select(valueName => new RequestValue(valueName, context.HandlingKernel.Name));



                foreach (var valueCommand in valueCommands)
                {
                    result = await context.HandlingKernel.SendAsync(valueCommand);
                    using var __ = result.KernelEvents.OfType<ValueProduced>().Subscribe(e => valueEvents.Add(e));
                }


                var kernelValues = valueEvents.Select(e => new KernelValue( new KernelValueInfo( e.Name, e.Value.GetType()), e.Value, context.HandlingKernel.Name));

                var currentVariables = new KernelValues(
                    kernelValues,
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

        public static CompositeKernel UseKernelClientConnection<TConnector>(
            this CompositeKernel kernel,
            ConnectKernelCommand<TConnector> command)
            where TConnector : KernelConnector
        {
            kernel.AddKernelConnection(command);

            return kernel;
        }

        [DebuggerStepThrough]
        public static T LogCommandsToPocketLogger<T>(this T kernel) 
            where T : Kernel
        {
            kernel.AddMiddleware(async (command, context, next) =>
            {
                using var _ = Logger.Log.OnEnterAndExit($"Command: {command.ToString().Replace(Environment.NewLine, " ")}");

                await next(command, context);
            });
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
            if (kernel is null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (onVisit is null)
            {
                throw new ArgumentNullException(nameof(onVisit));
            }

            foreach (var subKernel in kernel.Subkernels(recursive))
            {
                onVisit(subKernel);
            }
        }

        public static void VisitSubkernelsAndSelf(
            this Kernel kernel,
            Action<Kernel> onVisit,
            bool recursive = false)
        {
            if (kernel is null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (onVisit is null)
            {
                throw new ArgumentNullException(nameof(onVisit));
            }

            foreach (var k in kernel.SubkernelsAndSelf(recursive))
            {
                onVisit(k);
            }
        }

        public static async Task VisitSubkernelsAsync(
            this Kernel kernel,
            Func<Kernel, Task> onVisit,
            bool recursive = false)
        {
            if (kernel is null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (onVisit is null)
            {
                throw new ArgumentNullException(nameof(onVisit));
            }

            foreach (var subKernel in kernel.Subkernels(recursive))
            {
                await onVisit(subKernel);
            }
        }

        public static async Task VisitSubkernelsAndSelfAsync(
            this Kernel kernel,
            Func<Kernel, Task> onVisit,
            bool recursive = false)
        {
            if (kernel is null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (onVisit is null)
            {
                throw new ArgumentNullException(nameof(onVisit));
            }

            foreach (var k in kernel.SubkernelsAndSelf(recursive))
            {
                await onVisit(k);
            }
        }

        public static IEnumerable<Kernel> SubkernelsAndSelf(
            this Kernel kernel,
            bool recursive = false)
        {
            yield return kernel;

            if (kernel is CompositeKernel compositeKernel)
            {
                foreach (var subKernel in compositeKernel.ChildKernels)
                {
                    if (recursive)
                    {
                        foreach (var recursiveVisit in subKernel.SubkernelsAndSelf(recursive))
                        {
                            yield return recursiveVisit;
                        }
                    }
                    else
                    {
                        yield return subKernel;
                    }
                }
            }
        }

        public static IEnumerable<Kernel> Subkernels(
            this Kernel kernel,
            bool recursive = false)
        {
            if (kernel is CompositeKernel compositeKernel)
            {
                foreach (var subKernel in compositeKernel.ChildKernels)
                {
                    yield return subKernel;
                    if (recursive)
                    {
                        foreach (var recursiveVisit in subKernel.Subkernels(recursive))
                        {
                            yield return recursiveVisit;
                        }
                    }
                }
            }
        }
    }
}