// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;

using Pocket;

using CompletionItem = System.CommandLine.Completions.CompletionItem;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;

namespace Microsoft.DotNet.Interactive;

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

    public static Kernel FindKernelByName(this Kernel kernel, string name) => FindKernel(kernel, kernel => kernel.KernelInfo.NameAndAliases.Contains(name));

    public static Kernel FindKernel(this Kernel kernel, Func<Kernel, bool> predicate) => FindKernels(kernel, predicate).FirstOrDefault();

    public static IEnumerable<Kernel> FindKernels(this Kernel kernel, Func<Kernel, bool> predicate)
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
            CompositeKernel c => predicate(c) ? new[] { kernel }.Concat(c.ChildKernels.Where(predicate)) : c.ChildKernels.Where(predicate),
            _ when predicate(kernel) => new[] { kernel },
            _ => Enumerable.Empty<Kernel>()
        };
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

    public static TKernel UseImportMagicCommand<TKernel>(this TKernel kernel)
        where TKernel : Kernel
    {
        var fileArg = new Argument<FileInfo>("file").ExistingOnly();
        var command = new Command("#!import", "Runs another notebook or source code file inline.")
        {
            fileArg
        };

        command.SetHandler(async ctx =>
        {
            var file = ctx.ParseResult.GetValueForArgument(fileArg);
            await LoadAndRunInteractiveDocument(kernel, file);
        });

        kernel.AddDirective(command);

        return kernel;
    }

    public static async Task LoadAndRunInteractiveDocument(
        this Kernel kernel,
        FileInfo file)
    {
        var kernelInfoCollection = CreateKernelInfos(kernel.RootKernel as CompositeKernel);
        var document = await InteractiveDocument.LoadAsync(
                           file,
                           kernelInfoCollection);
        var lookup = kernelInfoCollection.ToDictionary(k => k.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var element in document.Elements)
        {
            if (lookup.TryGetValue(element.KernelName!, out var kernelInfo) &&
                StringComparer.OrdinalIgnoreCase.Equals(kernelInfo.LanguageName, "markdown"))
            {
                var formattedValue = new FormattedValue("text/markdown", element.Contents);
                await kernel.SendAsync(new DisplayValue(formattedValue));
            }
            else
            {
                await kernel.RootKernel.SendAsync(new SubmitCode(element.Contents, element.KernelName));
            }
        }

        static KernelInfoCollection CreateKernelInfos(CompositeKernel kernel)
        {
            KernelInfoCollection kernelInfos = new();

            foreach (var childKernel in kernel.ChildKernels)
            {
                kernelInfos.Add(new Documents.KernelInfo(childKernel.Name, languageName: childKernel.KernelInfo.LanguageName, aliases: childKernel.KernelInfo.Aliases));
            }

            if (!kernelInfos.Contains("markdown"))
            {
                kernelInfos = kernelInfos.Clone();
                kernelInfos.Add(new Documents.KernelInfo("markdown", languageName: "Markdown"));
            }

            return kernelInfos;
        }
    }

    public static T UseLogMagicCommand<T>(this T kernel)
        where T : Kernel
    {
        var command = new Command("#!log", "Enables session logging.");

        var logStarted = false;

        command.SetHandler(cmdLineContext =>
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
                        if (e is DiagnosticEvent or DisplayEvent or DiagnosticsProduced)
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

    private static async Task HandleSetMagicCommand<T>(
        T kernel,
        InvocationContext cmdLineContext,
        Option<string> nameOption,
        Option<object> valueOption,
        Option<string> mimeTypeOption,
        Option<bool> byrefOption)
        where T : Kernel
    {
        var valueName = cmdLineContext.ParseResult.GetValueForOption(nameOption);
        var mimeType = cmdLineContext.ParseResult.GetValueForOption(mimeTypeOption);
        var context = cmdLineContext.GetService<KernelInvocationContext>();
        var isByref = cmdLineContext.ParseResult.GetValueForOption(byrefOption);

        if (kernel.SupportsCommandType(typeof(SendValue)))
        {
            var events = new List<ValueProduced>();

            using var subscription = context.KernelEvents.OfType<ValueProduced>().Subscribe(events.Add);

            var valueProduced = events.SingleOrDefault();

            if (valueProduced is { })
            {
                var referenceValue = isByref ? valueProduced.Value : null;
                var formattedValue = valueProduced.FormattedValue;

                await SendValue(kernel, referenceValue, formattedValue, valueName);
            }
            else
            {
                var interpolatedValue = cmdLineContext.ParseResult.GetValueForOption(valueOption);

                await SendValue(kernel, interpolatedValue, null, valueName);
            }
        }
        else
        {
            context.Fail(context.Command, new CommandNotSupportedException(typeof(SendValue), kernel));
        }
    }

    public static T UseValueSharing<T>(this T kernel) where T : Kernel
    {
        ConfigureAndAddShareMagicCommand(kernel);
        ConfigureAndAddSetMagicCommand(kernel);
        return kernel;
    }

    private static void ConfigureAndAddSetMagicCommand<T>(T destinationKernel) where T : Kernel
    {
        var nameOption = new Option<string>(
            "--name",
            description: "The name of the value to be created in the current kernel.")
        {
            IsRequired = true
        };

        var byrefOption = new Option<bool>(
            "--byref",
            "Shares the specified value by reference if kernels are in the same process.");

        var mimeTypeOption = new Option<string>(
                "--mime-type", 
                description: "The MIME type by which the value should be represented. This will often determine how an object will be formatted into a string.",
                parseArgument: result =>
                {
                    if (result.GetValueForOption(byrefOption))
                    {
                        result.ErrorMessage = "The --mime-type and --byref options cannot be used together.";
                    }

                    return result.Tokens.FirstOrDefault()?.Value;
                })
            {
                ArgumentHelpName = "MIME-TYPE"
            }
            .AddCompletions(
                JsonFormatter.MimeType,
                HtmlFormatter.MimeType,
                PlainTextFormatter.MimeType);

        var valueOption = new Option<object>(
            "--value",
            description:
            "The value to be set. @input:user_prompt allows you to prompt the user for this value. Values can be requested from other kernels by name, for example @csharp:variableName.",
            parseArgument: ParseValueOption)
        {
            IsRequired = true,
            ArgumentHelpName = "@source:sourceValueName"
        };

        valueOption.AddCompletions(_ =>
        {
            if (destinationKernel.ParentKernel is { } composite)
            {
                var getValueTasks = composite.ChildKernels
                                             .Where(
                                                 k => k != destinationKernel &&
                                                      k.KernelInfo.SupportsCommand(nameof(RequestValueInfos)))
                                             .Select(async k => await k.SendAsync(new RequestValueInfos(k.Name)));

                var tasks = Task.WhenAll(getValueTasks).GetAwaiter().GetResult();

                var x = tasks
                        .Select(t => t.Events.OfType<ValueInfosProduced>())
                        .SelectMany(events => events.Select(e => new { e.Command, e.ValueInfos }))
                        .SelectMany(x =>
                        {
                            var kernelName = x.Command.TargetKernelName;

                            // TODO: (ConfigureAndAddSetMagicCommand) this is compensating for https://github.com/dotnet/interactive/issues/2728
                            if (kernelName is null &&
                                destinationKernel.RootKernel is CompositeKernel root &&
                                root.ChildKernels.TryGetByUri(x.Command.DestinationUri, out var k))
                            {
                                kernelName = k.Name;
                            }

                            return x.ValueInfos.Select(i => $"@{kernelName}:{i.Name}");
                        })
                        .OrderBy(x => x)
                        .Select(n => new CompletionItem(n))
                        .ToArray();

                return x;
            }

            return Array.Empty<CompletionItem>();
        });

        var set = new Command("#!set", "Sets a value in the current kernel")
        {
            nameOption,
            valueOption,
            mimeTypeOption,
            byrefOption
        };

        set.SetHandler(async cmdLineContext => 
                           await HandleSetMagicCommand(destinationKernel, cmdLineContext, nameOption, valueOption, mimeTypeOption, byrefOption));

        destinationKernel.AddDirective(set);

        object ParseValueOption(ArgumentResult argResult)
        {
            var valueOptionValue = argResult.Tokens.Single().Value;

            if (!valueOptionValue.StartsWith("@"))
            {
                return valueOptionValue;
            }

            bool isByref;
            var mimeTypeOptionResult = argResult.FindResultFor(mimeTypeOption);
            RequestValue requestValue;

            var (sourceKernelName, sourceValueName) = SubmissionParser.SplitKernelDesignatorToken(valueOptionValue[1..], destinationKernel.Name);

            if (argResult.GetValueForOption(byrefOption))
            {
                if (destinationKernel.KernelInfo.IsProxy)
                {
                    argResult.ErrorMessage = "Sharing by reference is not allowed when kernels are remote.";
                    return null;
                }

                if (destinationKernel.RootKernel.FindKernelByName(sourceKernelName) is { } sourceKernel &&
                    sourceKernel.KernelInfo.IsProxy)
                {
                    argResult.ErrorMessage = "Sharing by reference is not allowed when kernels are remote.";
                    return null;
                }

                requestValue = new RequestValue(sourceValueName, "text/plain", sourceKernelName);
                isByref = true;
            }
            else if (mimeTypeOptionResult is { ErrorMessage: null })
            {
                var mimeType = mimeTypeOptionResult.GetValueForOption(mimeTypeOption);
                requestValue = new RequestValue(sourceValueName, mimeType, sourceKernelName);
                isByref = false;
            }
            else
            {
                requestValue = new RequestValue(sourceValueName, JsonFormatter.MimeType, sourceKernelName);
                isByref = false;
            }

            var result = destinationKernel.RootKernel.SendAsync(requestValue).GetAwaiter().GetResult();

            if (result.Events.LastOrDefault() is CommandFailed failed)
            {
                argResult.ErrorMessage = failed.Message;
                return null;
            }

            var valueProduced = result.Events.OfType<ValueProduced>().Single();

            if (isByref)
            {
                return valueProduced.Value;
            }
            else
            {
                return valueProduced.FormattedValue;
            }
        }
    }

    private static void ConfigureAndAddShareMagicCommand<T>(T kernel) where T : Kernel
    {
        var sourceValueNameArg = new Argument<string>(
            "name",
            "The name of the value to share. (This is also the default name of the value created in the destination kernel, unless --as is used to specify a different one.)");

        sourceValueNameArg.AddCompletions(_ =>
        {
            if (kernel.ParentKernel is { } composite)
            {
                var getValueTasks = composite.ChildKernels
                                             .Where(
                                                 k => k != kernel &&
                                                      k.KernelInfo.SupportsCommand(nameof(RequestValueInfos)))
                                             .Select(async k => await k.SendAsync(new RequestValueInfos()));

                var tasks = Task.WhenAll(getValueTasks).GetAwaiter().GetResult();

                return tasks
                       .Select(t => t.Events.OfType<ValueInfosProduced>())
                       .SelectMany(events => events.SelectMany(e => e.ValueInfos))
                       .Select(vi => vi.Name)
                       .OrderBy(x => x)
                       .Select(n => new CompletionItem(n))
                       .ToArray();
            }

            return Array.Empty<CompletionItem>();
        });

        var fromKernelOption = new Option<string>(
            "--from",
            "The name of the kernel to get the value from.");

        fromKernelOption.AddCompletions(_ =>
        {
            if (kernel.ParentKernel is { } composite)
            {
                return composite.ChildKernels
                    .Where(k =>
                        k != kernel &&
                        k.KernelInfo.SupportsCommand(nameof(RequestValueInfos)) &&
                        k.KernelInfo.SupportsCommand(nameof(RequestValue)))
                    .Select(k => new CompletionItem(k.Name));
            }

            return Array.Empty<CompletionItem>();
        });

        var mimeTypeOption =
            new Option<string>("--mime-type", "Share the value as a string formatted to the specified MIME type.")
                .AddCompletions(
                    JsonFormatter.MimeType,
                    HtmlFormatter.MimeType,
                    PlainTextFormatter.MimeType);

        var asOption = new Option<string>("--as", "The name to give the the value in the importing kernel.");

        var share = new Command("#!share",
            "Get a value from one kernel and create a copy (or a reference if the kernels are in the same process) in another.")
        {
            fromKernelOption,
            sourceValueNameArg,
            mimeTypeOption,
            asOption
        };

        share.SetHandler(async cmdLineContext =>
        {
            var from = cmdLineContext.ParseResult.GetValueForOption(fromKernelOption);
            var valueName = cmdLineContext.ParseResult.GetValueForArgument(sourceValueNameArg);
            var context = cmdLineContext.GetService<KernelInvocationContext>();
            var mimeType = cmdLineContext.ParseResult.GetValueForOption(mimeTypeOption);
            var importAsName = cmdLineContext.ParseResult.GetValueForOption(asOption);

            if (kernel.FindKernelByName(from) is { } fromKernel)
            {
                await fromKernel.GetValueAndSendTo(
                    kernel,
                    valueName,
                    mimeType,
                    importAsName);
            }
            else
            {
                context.Fail(context.Command, message: $"Kernel not found: {from}");
            }
        });

        kernel.AddDirective(share);
    }

    internal static async Task GetValueAndSendTo(
        this Kernel fromKernel,
        Kernel toKernel,
        string fromName,
        string requestedMimeType,
        string toName)
    {
        var valueProduced = await GetValue(fromKernel, fromName, requestedMimeType);

        if (valueProduced is { })
        {
            var declarationName = toName ?? fromName;

            await SendValue(toKernel, requestedMimeType is not null, valueProduced, declarationName);
        }
    }

    private static async Task SendValue(Kernel kernel, bool ignoreReferenceValue, ValueProduced valueProduced,
        string declarationName)
    {
        if (kernel.SupportsCommandType(typeof(SendValue)))
        {
            var value =
                ignoreReferenceValue
                    ? null
                    : valueProduced.Value;

            await SendValue(kernel, value, valueProduced.FormattedValue, declarationName);
        }
        else
        {
            throw new CommandNotSupportedException(typeof(SendValue), kernel);
        }
    }

    private static async Task SendValue(Kernel kernel, object value, FormattedValue formattedValue,
        string declarationName)
    {
        if (kernel.SupportsCommandType(typeof(SendValue)))
        {
            await kernel.SendAsync(
                new SendValue(
                    declarationName,
                    value,
                    formattedValue));
        }
        else
        {
            throw new CommandNotSupportedException(typeof(SendValue), kernel);
        }
    }

    private static async Task<ValueProduced> GetValue(Kernel kernel, string name, string requestedMimeType)
    {
        var supportedRequestValue = kernel.SupportsCommandType(typeof(RequestValue));

        if (!supportedRequestValue)
        {
            throw new InvalidOperationException($"Kernel {kernel} does not support command {nameof(RequestValue)}");
        }

        var requestValueResult = await kernel.SendAsync(new RequestValue(name, mimeType: requestedMimeType));

        return requestValueResult.Events.OfType<ValueProduced>().SingleOrDefault();
    }

    public static TKernel UseWho<TKernel>(this TKernel kernel)
        where TKernel : Kernel
    {
        if (kernel.KernelInfo.SupportsCommand(nameof(RequestValueInfos)) &&
            kernel.KernelInfo.SupportsCommand(nameof(RequestValue)))
        {
            kernel.AddDirective(who());
            kernel.AddDirective(whos());
        }
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
                await DisplayValues(ctx.GetService<KernelInvocationContext>(), true);
            })
        };

        return command;
    }

    private static async Task DisplayValues(KernelInvocationContext context, bool detailed)
    {
        if (context.Command is SubmitCode &&
            context.HandlingKernel.KernelInfo.SupportsCommand(nameof(RequestValueInfos)) &&
            context.HandlingKernel.KernelInfo.SupportsCommand(nameof(RequestValue)))
        {
            var nameEvents = new List<ValueInfosProduced>();

            var result = await context.HandlingKernel.SendAsync(new RequestValueInfos(context.HandlingKernel.Name));
            nameEvents.AddRange(result.Events.OfType<ValueInfosProduced>());

            var valueNames = nameEvents.SelectMany(e => e.ValueInfos.Select(d => d.Name)).Distinct();

            var valueCommands = valueNames.Select(valueName => new RequestValue(valueName, targetKernelName: context.HandlingKernel.Name));

            var valueEvents = new List<ValueProduced>();
            foreach (var valueCommand in valueCommands)
            {
                result = await context.HandlingKernel.SendAsync(valueCommand);
                valueEvents.AddRange(result.Events.OfType<ValueProduced>());
            }

            var kernelValues =
                valueEvents.Select(e => new KernelValue(
                                       new KernelValueInfo(e.Name, new FormattedValue(PlainTextFormatter.MimeType, e.Value?.ToDisplayString(PlainTextFormatter.MimeType)),
                                                           e.Value?.GetType()), e.Value, context.HandlingKernel.Name));

            var currentVariables = new KernelValues(
                kernelValues,
                detailed);

            context.Publish(
                new DisplayedValueProduced(
                    currentVariables,
                    context.Command,
                    FormattedValue.FromObject(currentVariables)));
        }
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

    internal static bool TryRegisterForDisposal<T>(this Kernel kernel, T candidateDisposable)
    {
        if (candidateDisposable is IDisposable disposable)
        {
            kernel.RegisterForDisposal(disposable);
            return true;
        }

        return false;
    }
}