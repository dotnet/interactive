// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelExtensions
    {
        public static IKernel FindKernel(this IKernel kernel, string name)
        {
            var root = kernel
                       .RecurseWhileNotNull(k => k switch
                       {
                           KernelBase kb => kb.ParentKernel,
                           _ => null
                       })
                       .LastOrDefault();

            return root switch
            {
                _ when kernel.Name == name => kernel,
                CompositeKernel c => c.ChildKernels
                                      .SingleOrDefault(k => k.Name == name),
                _ => null
            };
        }

        public static Task<KernelCommandResult> SendAsync(
            this IKernel kernel,
            KernelCommand command)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return kernel.SendAsync(command, CancellationToken.None);
        }

        public static Task<KernelCommandResult> SubmitCodeAsync(
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
                            if (e is DiagnosticEvent || e is DisplayEvent)
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
            where T : DotNetLanguageKernel
        {
            var share = new Command("#!share", "Share a .NET variable between subkernels")
            {
                new Option<string>("--from", "The name of the kernel where the variable has been previously declared"),
                new Argument<string>("name", "The name of the variable to create in the destination kernel")
            };

            share.Handler = CommandHandler.Create<string, string, KernelInvocationContext>(async (from, name, context) =>
            {
                if (kernel.FindKernel(from) is DotNetLanguageKernel fromKernel)
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

        public static T UseProxyKernel<T>(this T kernel)
            where T : CompositeKernel
        {
            var command = new Command("#!connect", "Connect to the specified remote kernel.")
            {
                new Argument<string>("kernel-name"),
                new Argument<string>("remote-name")
            };

            command.Handler = CommandHandler.Create<string, string, KernelInvocationContext>(async (kernelName, remoteName, context) =>
            {
                var existingProxyKernel = kernel.FindKernel(kernelName);
                if (existingProxyKernel == null)
                {
                    var proxyKernel = new NamedPipeKernel(kernelName);
                    try
                    {
                        await proxyKernel.ConnectAsync(remoteName);
                        kernel.Add(proxyKernel);
                    }
                    catch
                    {
                        proxyKernel.Dispose();
                        throw;
                    }
                }
            });

            kernel.AddDirective(command);

            return kernel;
        }

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