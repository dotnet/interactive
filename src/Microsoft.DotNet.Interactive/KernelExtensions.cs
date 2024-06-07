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
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;

using CompletionItem = System.CommandLine.Completions.CompletionItem;

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

        if (kernel is CompositeKernel composite)
        {
            composite.SetDefaultTargetKernelNameForCommand(typeof(Quit), composite.Name);
        }

        return kernel;

        void ShutDown()
        {
            Environment.Exit(0);
        }
    }

    public static Kernel FindKernelByName(this Kernel kernel, string name) =>
        FindKernels(kernel, k => k.KernelInfo.NameAndAliases.Contains(name)).FirstOrDefault();

    public static IEnumerable<Kernel> FindKernels(this Kernel kernel, Func<Kernel, bool> predicate)
    {
        var root = kernel
            .RecurseWhileNotNull(k => k switch
            {
                { } => k.ParentKernel,
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
        var importDirective = new KernelActionDirective("#!import")
        {
            Parameters =
            {
                new("")
                {
                    AllowImplicitName = true,
                    Required = true
                }
            },
            TryGetKernelCommandAsync = ImportDocument.TryParseImportDirectiveAsync
        };

        kernel.AddDirective<ImportDocument>(
            importDirective,
            async (command, context) => await LoadAndRunInteractiveDocument(kernel, new FileInfo(command.FilePath)));

        return kernel;
    }

    public static async Task LoadAndRunInteractiveDocument(
        this Kernel kernel,
        FileInfo file,
        KernelCommand parentCommand = null)
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
                var displayValue = new DisplayValue(formattedValue);
                if (parentCommand is not null)
                {
                    displayValue.SetParent(parentCommand);
                }
                await kernel.SendAsync(displayValue);
            }
            else
            {
                var submitCode = new SubmitCode(element.Contents, element.KernelName);
                if (parentCommand is not null)
                {
                    submitCode.SetParent(parentCommand);
                }
                await kernel.RootKernel.SendAsync(submitCode);
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

    private static async Task HandleSetMagicCommand<T>(
        T kernel,
        InvocationContext cmdLineContext,
        Option<string> nameOption,
        Option<ValueOptionResult> valueOption,
        Option<bool> byrefOption)
        where T : Kernel
    {
        // FIX: (HandleSetMagicCommand) delete
        var context = cmdLineContext.GetService<KernelInvocationContext>();

        if (kernel.SupportsCommandType(typeof(SendValue)))
        {
            var valueProducedEvents = new List<ValueProduced>();

            var inputProducedEvents = new List<InputProduced>();

            using var subscription = context.KernelEvents
                                            .Where(e => e is ValueProduced or InputProduced)
                                            .Subscribe(
                                                e =>
                                                {
                                                    switch (e)
                                                    {
                                                        case ValueProduced vp:
                                                            valueProducedEvents.Add(vp);
                                                            break;
                                                        case InputProduced ip:
                                                            inputProducedEvents.Add(ip);
                                                            break;
                                                    }
                                                });

            var valueOptionResult = cmdLineContext.ParseResult.GetValueForOption(valueOption);

            var sourceKernelName = valueOptionResult.Kernel;
            
            var sourceKernel = Kernel.Root.FindKernelByName(sourceKernelName);

            ValueProduced valueProduced = null;

            if (valueOptionResult is { Name: var sourceValueName } && 
                sourceKernel is not null)
            {
                if (sourceKernel.KernelInfo.IsProxy == true)
                {
                    var destinationUri = sourceKernel.KernelInfo.RemoteUri;

                    valueProduced = valueProducedEvents.SingleOrDefault(e =>
                                                                            e.Name == sourceValueName && e.Command.DestinationUri == destinationUri);
                }
                else
                {
                    valueProduced = valueProducedEvents.SingleOrDefault(e =>
                                                                            e.Name == sourceValueName && e.Command.TargetKernelName == sourceKernelName);
                }
            }

            var valueNameFromCommandLine = cmdLineContext.ParseResult.GetValueForOption(nameOption);

            if (valueProduced is { })
            {
                var isByref = cmdLineContext.ParseResult.GetValueForOption(byrefOption);

                var referenceValue = isByref ? valueProduced.Value : null;
                var formattedValue = valueProduced.FormattedValue;

                await SendValue(context, kernel, referenceValue, formattedValue, valueNameFromCommandLine);
            }

            if (inputProducedEvents.Count > 0)
            {
                foreach (var inputProduced in inputProducedEvents)
                {
                    if (inputProduced.Command is RequestInput requestInput)
                    {
                        if (requestInput.IsPassword)
                        {
                            await SendValue(context, kernel, new PasswordString(inputProduced.Value), null, requestInput.ValueName);
                        }
                        else
                        {
                            await SendValue(context, kernel, inputProduced.Value, null, requestInput.ValueName);
                        }
                    }
                }
            }

            if (sourceKernelName is null)
            {
                if (inputProducedEvents.All(e => ((RequestInput)e.Command).ValueName != valueNameFromCommandLine))
                {
                    await SendValue(context, kernel, valueOptionResult.Value, null, valueNameFromCommandLine);
                }
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
        var directive = new KernelActionDirective("#!set")
        {
            KernelCommandType = typeof(SetDirectiveCommand),
            TryGetKernelCommandAsync = SetDirectiveCommand.TryParseSetDirectiveCommandAsync,
            Parameters =
            {
                new("--name")
                {
                    Required = true
                },
                new("--value")
                {
                    Required = true
                },
                new("--byref")
                {
                    Flag = true
                },
                new("--mime-type")
            }
        };

        destinationKernel.AddDirective<SetDirectiveCommand>(
            directive,
            SetDirectiveCommand.HandleAsync);

        var nameOption = new Option<string>(
            "--name",
            description: LocalizationResources.Magics_set_name_Description())
        {
            IsRequired = true
        };

        var byrefOption = new Option<bool>(
            "--byref",
            LocalizationResources.Magics_set_byref_Description());

        var mimeTypeOption = new Option<string>(
            "--mime-type",
            description: LocalizationResources.Magics_set_mime_type_Description(),
            parseArgument: result =>
            {
                if (result.GetValueForOption(byrefOption))
                {
                    result.ErrorMessage = LocalizationResources.Magics_set_mime_type_ErrorMessageCannotBeUsed();
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

        var valueOption = new Option<ValueOptionResult>(
            "--value",
            description:
            LocalizationResources.Magics_set_value_Description(),
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

        var set = new Command("#!set", LocalizationResources.Magics_set_Description())
        {
            nameOption,
            valueOption,
            mimeTypeOption,
            byrefOption
        };

        set.SetHandler(async cmdLineContext =>
                           await HandleSetMagicCommand(destinationKernel, cmdLineContext, nameOption, valueOption, byrefOption));

        // destinationKernel.AddDirective(set);

        ValueOptionResult ParseValueOption(ArgumentResult argResult)
        {
            var valueOptionValue = argResult.Tokens.Single().Value;

            if (!valueOptionValue.StartsWith("@"))
            {
                return new ValueOptionResult(valueOptionValue, null, null);
            }

            bool isByref;
            var mimeTypeOptionResult = argResult.FindResultFor(mimeTypeOption);
            RequestValue requestValue;

            var (sourceKernelName, sourceValueName) = SubmissionParser.SplitKernelDesignatorToken(valueOptionValue[1..], destinationKernel.Name);

            if (argResult.GetValueForOption(byrefOption))
            {
                if (destinationKernel.KernelInfo.IsProxy)
                {
                    argResult.ErrorMessage = LocalizationResources.Magics_set_ErrorMessageSharingByReference();
                    return null;
                }

                if (destinationKernel.RootKernel.FindKernelByName(sourceKernelName) is { } sourceKernel &&
                    sourceKernel.KernelInfo.IsProxy)
                {
                    argResult.ErrorMessage = LocalizationResources.Magics_set_ErrorMessageSharingByReference();
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

            if (KernelInvocationContext.Current is {} context)
            {
                requestValue.SetParent(context.Command);
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
                return new ValueOptionResult(valueProduced.Value, sourceKernelName, sourceValueName);
            }
            else
            {
                return new ValueOptionResult(valueProduced.FormattedValue, sourceKernelName, sourceValueName);
            }
        }
    }

    private record ValueOptionResult(object Value, string Kernel, string Name);

    private static void ConfigureAndAddShareMagicCommand<T>(T kernel) where T : Kernel
    {
        var shareDirective = new KernelActionDirective("#!share")
        {
            KernelCommandType = typeof(ShareDirectiveCommand),
            Parameters =
            {
                new("--name")
                {
                    AllowImplicitName = true
                },
                new("--from")
                {
                    Required = true
                },
                new("--as"),
                new("--mime-type")
            }
        };

        kernel.AddDirective<ShareDirectiveCommand>(
            shareDirective,
            ShareDirectiveCommand.HandleAsync);

        var sourceValueNameArg = new Argument<string>(
            "name",
            LocalizationResources.Magics_share_name_Description());

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
            LocalizationResources.Magics_share_from_Description());

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
            new Option<string>("--mime-type", LocalizationResources.Magics_share_mime_type_Description())
                .AddCompletions(
                    JsonFormatter.MimeType,
                    HtmlFormatter.MimeType,
                    PlainTextFormatter.MimeType);

        var asOption = new Option<string>("--as", LocalizationResources.Magics_share_as_Description());

        var share = new Command("#!share",
            LocalizationResources.Magics_share_Description())
        {
            fromKernelOption,
            sourceValueNameArg,
            mimeTypeOption,
            asOption
        };
    }

    internal static async Task GetValueAndSendTo(
        this Kernel fromKernel,
        KernelInvocationContext context,
        Kernel toKernel,
        string fromName,
        string requestedMimeType,
        string toName)
    {
        var supportedRequestValue = fromKernel.SupportsCommandType(typeof(RequestValue));

        if (!supportedRequestValue)
        {
            throw new InvalidOperationException($"Kernel {fromKernel} does not support command {nameof(RequestValue)}");
        }

        var requestValue = new RequestValue(fromName, mimeType: requestedMimeType);

        requestValue.SetParent(context.Command, true);

        var requestValueResult = await fromKernel.SendAsync(requestValue);
        var valueProduced = requestValueResult.Events.OfType<ValueProduced>().SingleOrDefault();

        if (valueProduced is not null)
        {
            var declarationName = toName ?? fromName;

            bool ignoreReferenceValue = requestedMimeType is not null;

            if (toKernel.SupportsCommandType(typeof(SendValue)))
            {
                var value =
                    ignoreReferenceValue
                        ? null
                        : valueProduced.Value;

                await SendValue(context, toKernel, value, valueProduced.FormattedValue, declarationName);
            }
            else
            {
                throw new CommandNotSupportedException(typeof(SendValue), toKernel);
            }
        }
    }

    internal static async Task SendValue(
        KernelInvocationContext context,
        Kernel kernel, 
        object value, 
        FormattedValue formattedValue,
        string declarationName)
    {
        if (kernel.SupportsCommandType(typeof(SendValue)))
        {
            var sendValue = new SendValue(
                declarationName,
                value,
                formattedValue);

            sendValue.SetParent(context.Command, true);

            await kernel.SendAsync(sendValue);
        }
        else
        {
            throw new CommandNotSupportedException(typeof(SendValue), kernel);
        }
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
        var command = new Command(name: "#!who", LocalizationResources.Magics_who_Description())
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
        var command = new Command("#!whos", LocalizationResources.Magics_whos_Description())
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
                    FormattedValue.CreateManyFromObject(currentVariables)));
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