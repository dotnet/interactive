// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;
using Pocket;
using CompletionItem = System.CommandLine.Completions.CompletionItem;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelExtensions
    {
        public static T UseQuitCommand<T>(this T kernel, Func<Task> onQuitAsync = null) where T : Kernel
        {
            kernel.RegisterCommandHandler<Quit>(async (_, _) =>
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

        [DebuggerStepThrough]
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

            command.Handler = CommandHandler.Create((InvocationContext cmdLineContext) =>
            {
                if (logStarted)
                {
                    return Task.CompletedTask;
                }

                var context = cmdLineContext.GetService<KernelInvocationContext>();

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

                return Task.CompletedTask;

                static void PublishLogEvent(KernelInvocationContext c, string message) => c.Publish(new DiagnosticLogEntryProduced(message, c.Command));
            });

            kernel.AddDirective(command);

            return kernel;
        }

        public static ProxyKernel UseValueSharing(
            this ProxyKernel kernel,
            IKernelValueDeclarer kernelValueDeclarer)
        {
            if (kernelValueDeclarer is null)
            {
                throw new ArgumentNullException(nameof(kernelValueDeclarer));
            }

            kernel.UseValueSharing();
            kernel.ValueDeclarer = kernelValueDeclarer;
            return kernel;
        }

        public static T UseValueSharing<T>(this T kernel) where T : Kernel
        {
            var valueNameArg = new Argument<string>(
                "name",
                "The name of the variable to create in the destination kernel");

            valueNameArg.AddCompletions(_ =>
            {
                if (kernel.ParentKernel is { } composite)
                {
                    return composite.ChildKernels
                                    .OfType<ISupportGetValue>()
                                    .SelectMany(k => k.GetValueInfos().Select(vd => vd.Name))
                                    .Select(n => new CompletionItem(n))
                                    .ToArray();
                }

                return Array.Empty<CompletionItem>();
            });

            var fromKernelOption = new Option<string>(
                "--from",
                "The name of the kernel where the variable has been previously declared");

            fromKernelOption.AddCompletions(_ =>
            {
                if (kernel.ParentKernel is { } composite)
                {
                    return composite.ChildKernels
                                    .Where(k => k is ISupportGetValue)
                                    .Select(k => new CompletionItem(k.Name));
                }

                return Array.Empty<CompletionItem>();
            });

            var share = new Command("#!share", "Share a value between subkernels")
            {
                fromKernelOption,
                valueNameArg
            };

            share.Handler = CommandHandler.Create(async (InvocationContext cmdLineContext) =>
            {
                var from = cmdLineContext.ParseResult.GetValueForOption(fromKernelOption);
                var valueName = cmdLineContext.ParseResult.GetValueForArgument(valueNameArg);
                var context = cmdLineContext.GetService<KernelInvocationContext>();

                if (kernel.FindKernel(from) is { } fromKernel)
                {
                    await fromKernel.GetValueAndImportTo(kernel, valueName);
                }
                else
                {
                    context.Fail(context.Command, message: $"Kernel not found: {from}");
                }
            });

            kernel.AddDirective(share);

            return kernel;
        }

        internal static async Task GetValueAndImportTo(
            this Kernel fromKernel,
            Kernel toKernel,
            string valueName)
        {
            var supportedRequestValue = fromKernel.SupportsCommandType(typeof(RequestValue));

            if (!supportedRequestValue)
            {
                throw new InvalidOperationException($"Kernel {fromKernel} does not support command {nameof(RequestValue)}");
            }

            var requestValueResult = await fromKernel.SendAsync(new RequestValue(valueName));

            if (requestValueResult.KernelEvents.ToEnumerable().OfType<ValueProduced>().SingleOrDefault() is { } valueProduced)
            {
                await DeclareValue(
                    toKernel,
                    valueProduced);
            }
        }

        private static async Task DeclareValue(
            Kernel importingKernel,
            ValueProduced valueProduced)
        {
            var valueName = valueProduced.Name;

            if (importingKernel is ISupportSetClrValue toInProcessKernel)
            {
                if (valueProduced.Value is not { } value)
                {
                    if (valueProduced.FormattedValue.MimeType == JsonFormatter.MimeType)
                    {
                        var jsonDoc = JsonDocument.Parse(valueProduced.FormattedValue.Value);

                        value = jsonDoc.RootElement.ValueKind switch
                        {
                            JsonValueKind.Object => jsonDoc,
                            JsonValueKind.Array => jsonDoc,

                            JsonValueKind.Undefined => null,
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => null,
                            JsonValueKind.String => jsonDoc.Deserialize<string>(),
                            JsonValueKind.Number => jsonDoc.Deserialize<double>(),

                            _ => throw new ArgumentOutOfRangeException()
                        };
                    }
                    else
                    {
                        throw new ArgumentException($"Unable to import value '{valueName}' into kernel {importingKernel}");
                    }
                }

                await toInProcessKernel.SetValueAsync(valueName, value);

                return;
            }

            var declarer = importingKernel.GetValueDeclarer();

            if (declarer.TryGetValueDeclaration(valueProduced, out KernelCommand command))
            {
                await importingKernel.SendAsync(command);
            }
            else
            {
                throw new ArgumentException($"Value '{valueName}' cannot be declared in kernel '{importingKernel.Name}'");
            }
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
                Handler = CommandHandler.Create(async (InvocationContext ctx) =>
                {
                    await DisplayValues(ctx.GetService<KernelInvocationContext>(), false);
                })
            };

            return command;
        }

        private static Command whos()
        {
            var command = new Command("#!whos", "Display the names of the current top-level variables and their values.")
            {
                Handler = CommandHandler.Create(async (InvocationContext ctx) =>
                {
                    await DisplayValues(
                        ctx.GetService<KernelInvocationContext>(), true);
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

                var kernelValues = valueEvents.Select(e => new KernelValue(new KernelValueInfo(e.Name, e.Value.GetType()), e.Value, context.HandlingKernel.Name));

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