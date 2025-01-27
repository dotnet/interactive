// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.App.Commands;
using Microsoft.DotNet.Interactive.App.Events;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PackageManagement;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Telemetry;
using static Microsoft.DotNet.Interactive.App.CodeExpansion;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.App;

public static class KernelExtensions
{
    public static CompositeKernel UseCodeExpansions(
        this CompositeKernel kernel,
        CodeExpansionConfiguration config)
    {
        if (kernel is null)
        {
            throw new ArgumentNullException(nameof(kernel));
        }

        KernelEventEnvelope.RegisterEvent<CodeExpansionInfosProduced>();

        kernel.RegisterCommandHandler<RequestCodeExpansionInfos>(async (request, context) =>
        {
            var infos = await config.GetCodeExpansionInfosAsync();

            CodeExpansionInfosProduced infosProduced = new(infos, request);

            context.Publish(infosProduced);
        });

        // Register for the event notifying us when a kernel connection is established
        var subscription = kernel.KernelEvents
                                 .OfType<KernelInfoProduced>()
                                 .Subscribe(produced =>
                                 {
                                     if (produced.ConnectionShortcutCode is not null)
                                     {
                                         // FIX: (UseCodeExpansions) can we determine if there's a #r nuget needed for this to reproducible?
                                         if (config.GetRecentConnections is not null)
                                         {
                                             var recentConnectionList = config.GetRecentConnections();
                                             var expansionSubmission = new CodeExpansionSubmission(produced.ConnectionShortcutCode, produced.Command.TargetKernelName);
                                             var codeExpansion = new CodeExpansion(
                                                 [expansionSubmission],
                                                 new CodeExpansionInfo(produced.KernelInfo.LocalName, CodeExpansionKind.RecentConnection));

                                             recentConnectionList.Add(codeExpansion);

                                             config.AddCodeExpansion(codeExpansion);

                                             if (config.SaveRecentConnections is not null)
                                             {
                                                 config.SaveRecentConnections(recentConnectionList);
                                             }
                                         }
                                     }
                                 });

        var expandDirective = new KernelActionDirective("#!expand")
        {
            Parameters =
            [
                new KernelDirectiveParameter("--name", "The name of the code expansion.")
                {
                    AllowImplicitName = true,
                    Required = true
                },
                new KernelDirectiveParameter("--insert-at-position", "The index of the cell after which to insert the expanded code.")
            ],
            Hidden = true
        };

        kernel.AddDirective<ExpandCode>(
            expandDirective,
            async (expandCode, context) =>
            {
                var codeExpansion = await config.GetCodeExpansionAsync(expandCode.Name);
                if (codeExpansion is null)
                {
                    context.Fail(expandCode, message: $"No code expansion named '{expandCode.Name}' was found.");
                    return;
                }

                var offset = 0;
                foreach (var submission in codeExpansion.Content)
                {
                    await kernel.SendAsync(new SendEditableCode(
                                               submission.TargetKernelName,
                                               submission.Code)
                    {
                        InsertAtPosition = expandCode.InsertAtPosition + offset
                    });

                    offset++;
                }
            });

        kernel.RegisterForDisposal(subscription);

        return kernel;
    }

    public static TKernel UseFormsForMultipleInputs<TKernel>(
        this TKernel kernel,
        SecretManager secretManager = null)
        where TKernel : Kernel
    {
        if (kernel.SupportsCommandType(typeof(SendValue)))
        {
            throw new InvalidOperationException($"A command handler for {nameof(SendValue)} is already registered on kernel {kernel.Name}.");
        }

        var barrier = new Barrier(2);
        kernel.RegisterForDisposal(barrier);
        ConcurrentDictionary<string, FormattedValue> receivedValues = new(StringComparer.OrdinalIgnoreCase);

        kernel.RegisterCommandHandler<RequestInputs>(async (requestInputs, context) =>
        {
            var formId = Guid.NewGuid().ToString("N");

            var inputDescriptions = requestInputs.Inputs;

            PocketView html = div(
                form[id: formId](
                    inputDescriptions.Select(GetHtmlForSingleInput),
                    button[onclick: $"event.preventDefault(); sendSendValueCommand(document.getElementById('{formId}'));"]("Ok")));

            PocketView GetHtmlForSingleInput(InputDescription inputDescription)
            {
                var inputName = inputDescription.GetPropertyNameForJsonSerialization();

                var value = "";

                if (inputDescription.SaveAs is not null &&
                    secretManager is not null)
                {
                    secretManager.TryGetValue(inputDescription.SaveAs, out value);
                }

                return div(
                    label[@for: inputName](inputDescription.Prompt),
                    br,
                    input[
                        "required",
                        type: inputDescription.TypeHint,
                        id: inputName,
                        name: inputName,
                        value: value,
                        onkeydown: "event.stopPropagation()" // prevent event bubbling from triggering (for example) key commands in VS Code
                    ]());
            }

            context.Display(html);

            await Task.Yield();

            barrier.SignalAndWait(context.CancellationToken);

            if (receivedValues.TryGetValue(formId, out var formattedValue))
            {
                var values = JsonSerializer.Deserialize<Dictionary<string, string>>(formattedValue.Value);

                if (secretManager is not null)
                {
                    foreach (var inputDescription in inputDescriptions)
                    {
                        if (inputDescription.SaveAs is not null)
                        {
                            if (values.TryGetValue(inputDescription.GetPropertyNameForJsonSerialization(), out var value))
                            {
                                secretManager.SetValue(inputDescription.SaveAs, value);
                            }
                        }
                    }
                }

                context.Publish(new InputsProduced(
                                    values,
                                    requestInputs));
            }
            else
            {
                context.Fail(requestInputs, message: "No input received.");
            }
        });
        kernel.RegisterCommandHandler<SendValue>((sendValue, context) =>
        {
            receivedValues[sendValue.Name] = sendValue.FormattedValue;

            // don't wait on the barrier if the form hasn't been displayed 
            if (barrier.ParticipantsRemaining == 1)
            {
                barrier.SignalAndWait(context.CancellationToken);
            }

            return Task.CompletedTask;
        });

        return kernel;
    }

    public static CSharpKernel UseNugetDirective(this CSharpKernel kernel, bool forceRestore = false)
    {
        kernel.UseNugetDirective((k, resolvedPackageReference) =>
        {
            k.AddAssemblyReferences(resolvedPackageReference
                                        .SelectMany(r => r.AssemblyPaths));
            return Task.CompletedTask;
        }, forceRestore);

        return kernel;
    }

    public static FSharpKernel UseNugetDirective(this FSharpKernel kernel, bool forceRestore = false)
    {
        kernel.UseNugetDirective((k, resolvedPackageReference) =>
        {
            var resolvedAssemblies = resolvedPackageReference
                .SelectMany(r => r.AssemblyPaths);

            var packageRoots = resolvedPackageReference
                .Select(r => r.PackageRoot);

            k.AddAssemblyReferencesAndPackageRoots(resolvedAssemblies, packageRoots);

            return Task.CompletedTask;
        }, forceRestore);

        return kernel;
    }

    public static T UseAboutMagicCommand<T>(this T kernel)
        where T : Kernel
    {
        var aboutDirective = new KernelActionDirective("#!about")
        {
            Description = LocalizationResources.Magics_about_Description()
        };

        kernel.AddDirective(
            aboutDirective,
            (_, context) =>
            {
                context.Display(BuildInfo.GetBuildInfo(typeof(Program).Assembly));
                return Task.CompletedTask;
            }
        );

        Formatter.Register<BuildInfo>((info, writer) =>
        {
            var url = "https://github.com/dotnet/interactive";
            var encodedImage = string.Empty;

            var assembly = typeof(KernelExtensions).Assembly;
            using (var resourceStream = assembly.GetManifestResourceStream($"{typeof(KernelExtensions).Namespace}.resources.logo-456x456.png"))
            {
                if (resourceStream is not null)
                {
                    var png = new byte[resourceStream.Length];
                    var _ = resourceStream.Read(png, 0, png.Length);
                    encodedImage = $"data:image/png;base64, {Convert.ToBase64String(png)}";
                }
            }

            var libraryAssembly = typeof(Kernel).Assembly;
            var libraryInformationalVersion = libraryAssembly
                                              .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                              .InformationalVersion;

            PocketView html = table(
                tbody(
                    tr(
                        td(img[src: encodedImage, width: "125em"]),
                        td[style: "line-height:.8em"](
                            p[style: "font-size:1.5em"](b(".NET Interactive")),
                            p("© 2020-2025 Microsoft Corporation"),
                            p(b("Version: "), info.AssemblyInformationalVersion),
                            p(b("Library version: "), libraryInformationalVersion),
                            p(a[href: url](url))
                        ))
                ));

            writer.Write(html);
        }, HtmlFormatter.MimeType);

        return kernel;
    }

    public static CompositeKernel UseSecretManager(
        this CompositeKernel kernel,
        SecretManager secretManager)
    {
        if (secretManager is null)
        {
            throw new ArgumentNullException(nameof(secretManager));
        }

        kernel.AddMiddleware(async (command, context, next) =>
        {
            if (command is not RequestInput { SaveAs: { } saveAs } requestInput)
            {
                await next(command, context);
                return;
            }

            if (secretManager.TryGetValue(requestInput.SaveAs, out var value))
            {
                context.Publish(new InputProduced(value, requestInput));

                var message =
                    $"""
                     Using previously saved value for `{requestInput.SaveAs}`.

                     {MoreInfoMessage()}
                     """;
                context.Publish(new DisplayedValueProduced(
                                    message,
                                    requestInput,
                                    [new FormattedValue("text/markdown", message)]));
            }
            else
            {
                using var _ = context.KernelEvents.Subscribe(@event =>
                {
                    if (@event is InputProduced inputProduced &&
                        inputProduced.Command.GetOrCreateToken() == requestInput.GetOrCreateToken())
                    {
                        secretManager.SetValue(requestInput.SaveAs, inputProduced.Value);

                        var message =
                            $"""
                             Your response for value `{saveAs}` has been saved and will be reused without a prompt in the future. 

                             {MoreInfoMessage()}
                             """;
                        context.Publish(new DisplayedValueProduced(
                                            message,
                                            requestInput,
                                            [new FormattedValue("text/markdown", message)]));
                    }
                });

                await next(command, context);
            }

            string MoreInfoMessage() =>
                $"""
                 > 💡 To remove this value from your [SecretStore](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.secretstore/?view=ps-modules), run the following command in a PowerShell cell:
                 > 
                 > ```powershell
                 >     Remove-Secret -Name "{requestInput.SaveAs}" -Vault {secretManager.VaultName}
                 > ```

                 > 📝 For more information, see [SecretManagement](https://learn.microsoft.com/en-us/powershell/utility-modules/secretmanagement/overview?view=ps-modules).
                 """;
        });

        return kernel;
    }

    public static CompositeKernel UseTelemetrySender(
        this CompositeKernel kernel,
        TelemetrySender telemetrySender)
    {
        var executionOrder = 0;
        var sessionId = Guid.NewGuid().ToString();
        var subscription = kernel.KernelEvents.Subscribe(SendTelemetryFor);

        kernel.AddMiddleware(async (command, context, next) =>
        {
            await next(command, context);

            if (command is SubmitCode)
            {
                var properties = GetStandardPropertiesFromCommand(command);

                telemetrySender.TrackEvent(
                    "CodeSubmitted",
                    properties: properties);
            }
        });

        kernel.RegisterForDisposal(subscription);

        return kernel;

        void SendTelemetryFor(KernelEvent @event)
        {
            switch (@event.Command, kernelEvent: @event)
            {
                case (_, PackageAdded added):
                {
                    var properties = GetStandardPropertiesFromEvent(@event);
                    properties.Add("PackageName", added.PackageReference.PackageName.ToSha256HashWithNormalizedCasing());
                    properties.Add("PackageVersion", added.PackageReference.PackageVersion.ToSha256Hash());

                    var measurements = GetStandardMeasurementsFromEvent(@event);

                    telemetrySender.TrackEvent(
                        "PackageAdded",
                        measurements: measurements,
                        properties: properties);
                }
                    break;
            }
        }

        Dictionary<string, string> GetStandardPropertiesFromEvent(KernelEvent kernelEvent)
        {
            return GetStandardPropertiesFromCommand(kernelEvent.Command);
        }

        Dictionary<string, string> GetStandardPropertiesFromCommand(KernelCommand kernelCommand)
        {
            Kernel handlingKernel = null;
            if (kernelCommand.TargetKernelName is not null)
            {
                kernel.ChildKernels.TryGetByAlias(kernelCommand.TargetKernelName, out handlingKernel);
            }

            var properties = new Dictionary<string, string>
            {
                ["KernelName"] = kernelCommand.TargetKernelName?.ToSha256Hash(),
                ["KernelLanguageName"] = handlingKernel?.KernelInfo?.LanguageName?.ToSha256Hash(),
                ["KernelSessionId"] = sessionId
            };
            return properties;
        }

        Dictionary<string, double> GetStandardMeasurementsFromEvent(KernelEvent @event)
        {
            return new Dictionary<string, double>
            {
                ["ExecutionOrder"] = ++executionOrder,
                ["Succeeded"] = @event is CommandSucceeded ? 1 : 0
            };
        }
    }
}