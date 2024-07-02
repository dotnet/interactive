// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;

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
            Description = LocalizationResources.Magics_import_Description(),
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
            Description = LocalizationResources.Magics_set_name_Description(),
            KernelCommandType = typeof(SetDirectiveCommand),
            TryGetKernelCommandAsync = SetDirectiveCommand.TryParseSetDirectiveCommandAsync,
            Parameters =
            {
                new("--name")
                {
                    Description = LocalizationResources.Magics_set_name_Description(),
                    Required = true
                },
                new KernelDirectiveParameter("--value")
                {
                    Description = LocalizationResources.Magics_set_value_Description(),
                    Required = true
                }.AddCompletions(async _ =>
                {
                    if (destinationKernel.ParentKernel is { } composite)
                    {
                        var getValueTasks = composite.ChildKernels
                                                     .Where(
                                                         k => k != destinationKernel &&
                                                              k.KernelInfo.SupportsCommand(nameof(RequestValueInfos)))
                                                     .Select(async k => await k.SendAsync(new RequestValueInfos(k.Name)));

                        var tasks = await Task.WhenAll(getValueTasks);

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
                                .Select(n => new CompletionItem(n, WellKnownTags.Parameter))
                                .ToArray();

                        return x;
                    }

                    return Array.Empty<CompletionItem>();
                }),
                new("--byref")
                {
                    Description = LocalizationResources.Magics_set_byref_Description(),
                    Flag = true
                },
                new KernelDirectiveParameter("--mime-type")
                {
                    Description = LocalizationResources.Magics_set_mime_type_Description()
                }.AddCompletions(
                    _ =>
                    [
                        JsonFormatter.MimeType,
                        HtmlFormatter.MimeType,
                        PlainTextFormatter.MimeType
                    ])
            }
        };

        destinationKernel.AddDirective<SetDirectiveCommand>(
            directive,
            SetDirectiveCommand.HandleAsync);
    }

    private static void ConfigureAndAddShareMagicCommand<T>(T kernel) where T : Kernel
    {
        var shareDirective = new KernelActionDirective("#!share")
        {
            Description = LocalizationResources.Magics_share_Description(),
            KernelCommandType = typeof(ShareDirectiveCommand),
            Parameters =
            {
                new KernelDirectiveParameter("--name")
                {
                    AllowImplicitName = true,
                    Description = LocalizationResources.Magics_share_name_Description(),
                }.AddCompletions(async _ =>
                {
                    if (kernel.ParentKernel is { } composite)
                    {
                        var getValueTasks = composite.ChildKernels
                                                     .Where(
                                                         k => k != kernel &&
                                                              k.KernelInfo.SupportsCommand(nameof(RequestValueInfos)))
                                                     .Select(async k => await k.SendAsync(new RequestValueInfos()));

                        var tasks = await Task.WhenAll(getValueTasks);

                        return tasks
                               .Select(t => t.Events.OfType<ValueInfosProduced>())
                               .SelectMany(events => events.SelectMany(e => e.ValueInfos))
                               .Select(vi => vi.Name)
                               .OrderBy(x => x)
                               .Select(n => new CompletionItem(n, WellKnownTags.Parameter))
                               .ToArray();
                    }

                    return Array.Empty<CompletionItem>();
                }),
                new KernelDirectiveParameter("--from")
                {
                    Description = LocalizationResources.Magics_share_from_Description(),
                    Required = true
                }.AddCompletions(_ =>
                {
                    if (kernel.ParentKernel is { } composite)
                    {
                        return composite.ChildKernels
                                        .Where(k =>
                                                   k != kernel &&
                                                   k.KernelInfo.SupportsCommand(nameof(RequestValueInfos)) &&
                                                   k.KernelInfo.SupportsCommand(nameof(RequestValue)))
                                        .Select(k => new CompletionItem(k.Name, WellKnownTags.Parameter));
                    }

                    return Array.Empty<CompletionItem>();
                }),
                new("--as")
                {
                    Description = LocalizationResources.Magics_share_as_Description()
                },
                new KernelDirectiveParameter("--mime-type")
                {
                    Description = LocalizationResources.Magics_share_mime_type_Description()
                }.AddCompletions(
                    _ =>
                    [
                        JsonFormatter.MimeType,
                        HtmlFormatter.MimeType,
                        PlainTextFormatter.MimeType
                    ])
            }
        };

        kernel.AddDirective<ShareDirectiveCommand>(
            shareDirective,
            ShareDirectiveCommand.HandleAsync);
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
            kernel.AddDirective(new KernelActionDirective("#!who") { Description = LocalizationResources.Magics_who_Description() },
                                async (_, context) => await DisplayValues(context, false));
            kernel.AddDirective(new KernelActionDirective("#!whos") { Description = LocalizationResources.Magics_whos_Description() },
                                async (_, context) => await DisplayValues(context, true));
        }

        return kernel;
    }

    private static async Task DisplayValues(KernelInvocationContext context, bool detailed)
    {
        if (context.HandlingKernel.KernelInfo.SupportsCommand(nameof(RequestValueInfos)) &&
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
        Action<Kernel> onVisit)
    {
        if (kernel is null)
        {
            throw new ArgumentNullException(nameof(kernel));
        }

        if (onVisit is null)
        {
            throw new ArgumentNullException(nameof(onVisit));
        }

        if (kernel is CompositeKernel compositeKernel)
        {
            foreach (var subKernel in compositeKernel)
            {
                onVisit(subKernel);
            }
        }
    }

    public static void VisitSubkernelsAndSelf(
        this Kernel kernel,
        Action<Kernel> onVisit)
    {
        if (kernel is null)
        {
            throw new ArgumentNullException(nameof(kernel));
        }

        if (onVisit is null)
        {
            throw new ArgumentNullException(nameof(onVisit));
        }

        foreach (var k in kernel.SubkernelsAndSelf())
        {
            onVisit(k);
        }
    }

    internal static IEnumerable<Kernel> SubkernelsAndSelf(this Kernel kernel)
    {
        yield return kernel;

        if (kernel is CompositeKernel compositeKernel)
        {
            foreach (var subKernel in compositeKernel.ChildKernels)
            {
                yield return subKernel;
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