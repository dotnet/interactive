// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;
using Microsoft.DotNet.Interactive.Utility;

using Pocket;

using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;

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
                CompositeKernel c => c.ChildKernels
                                      .SingleOrDefault(k => k.Name == name),
                _ => null
            };
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

        public static CompositeKernel UseProxyKernelWithNamedPipe(this CompositeKernel kernel)
        {
            var connectionCommand = new Command("named-pipe");
            connectionCommand.AddArgument(new Argument<string>("kernel-name"));
            connectionCommand.AddArgument(new Argument<string>("pipe-name"));

            connectionCommand.Handler = CommandHandler.Create<string, string, KernelInvocationContext>(async (kernelName, pipeName, context) =>
            {
                var existingProxyKernel = kernel.FindKernel(kernelName);
                if (existingProxyKernel == null)
                {
                    var clientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation);
                    await clientStream.ConnectAsync();
                    clientStream.ReadMode = PipeTransmissionMode.Message;
                    var client = NamedPipeTransport.CreateClient(clientStream);
                    var proxyKernel = new ProxyKernel(kernelName, client);
                    kernel.Add(proxyKernel);

                }
            });
            kernel.ConfigureConnection(connectionCommand);
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
    }
}