// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Pocket;
using static System.CommandLine.Rendering.Ansi.Color;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelExtensions
    {
        private static readonly TextSpanFormatter _textSpanFormatter = new TextSpanFormatter();

        public static Task<IKernelCommandResult> SendAsync(
            this IKernel kernel,
            IKernelCommand command)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return kernel.SendAsync(command, CancellationToken.None);
        }

        public static Task<IKernelCommandResult> SubmitCodeAsync(
            this IKernel kernel, 
            string code)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return kernel.SendAsync(new SubmitCode(code), CancellationToken.None);
        }

        public static T UseLog<T>(this T kernel)
            where T : KernelBase
        {
            var command = new Command("#!log", "Enables session logging.");


            var logStarted = false;

            command.Handler = CommandHandler.Create<KernelInvocationContext>(async context =>
            {
                if (logStarted)
                {
                    return;
                }

                logStarted = true;

                kernel.AddMiddleware(async (kernelCommand, context, next) =>
                {
                    await Log(context, kernelCommand.ToLogString());

                    await next(kernelCommand, context);
                });

                var disposable = new CompositeDisposable
                {
                    kernel.KernelEvents.Subscribe(async e =>
                    {
                        if (KernelInvocationContext.Current is {} currentContext)
                        {
                            if (!(e is DisplayEventBase))
                            {
                                await Log(currentContext, e.ToLogString());
                            }
                        }
                    }),
                    LogEvents.Subscribe(async e =>
                    {
                        if (KernelInvocationContext.Current is {} currentContext)
                        {
                            await Log(currentContext, e.ToLogString());
                        }
                    })
                };

                kernel.RegisterForDisposal(disposable);

                await Log(context, "Logging enabled");

                Task Log(KernelInvocationContext c, string message) => c.DisplayAnsi($"{Foreground.LightGray}{message}{Off}");
            });

            kernel.AddDirective(command);

            return kernel;
        }

        internal static Task DisplayAnsi(
            this KernelInvocationContext context, 
            FormattableString message) =>
            DisplayAnsi(context, _textSpanFormatter.ParseToSpan(message));

        internal static Task DisplayAnsi(KernelInvocationContext context, TextSpan span) => 
            context.DisplayAsync(span, PlainTextFormatter.MimeType);

        [DebuggerStepThrough]
        public static T LogEventsToPocketLogger<T>(this T kernel)
            where T : IKernel
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

            if (kernel is KernelBase kernelBase)
            {
                kernelBase.RegisterForDisposal(disposables);
            }

            return kernel;
        }

        public static void VisitSubkernels(
            this IKernel kernel,
            Action<IKernel> onVisit,
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

        public static async Task VisitSubkernelsAsync(
            this IKernel kernel,
            Func<IKernel, Task> onVisit,
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
    }
}