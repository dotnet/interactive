// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.App.Connection;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Mermaid;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Telemetry;
using Pocket;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

namespace Microsoft.DotNet.Interactive.App;

public static class KernelBuilder
{
    internal static CompositeKernel CreateKernel(
        string defaultKernelName,
        FrontendEnvironment frontendEnvironment,
        StartupOptions startupOptions,
        TelemetrySender telemetrySender)
    {
        using var _ = Logger.Log.OnEnterAndExit("Creating Kernels");

        var compositeKernel = new CompositeKernel();
        compositeKernel.FrontendEnvironment = frontendEnvironment;

        // TODO: temporary measure to support vscode integrations
        compositeKernel.Add(new SqlDiscoverabilityKernel());
        compositeKernel.Add(new KqlDiscoverabilityKernel());

        compositeKernel.Add(
            new CSharpKernel()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseWho()
                .UseValueSharing(),
            ["c#", "C#"]);

        compositeKernel.Add(
            new FSharpKernel()
                .UseDefaultFormatting()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseWho()
                .UseValueSharing(),
            ["f#", "F#"]);

        var powerShellKernel = new PowerShellKernel()
                               .UseProfiles()
                               .UseValueSharing();
        compositeKernel.Add(
            powerShellKernel,
            ["powershell"]);

        compositeKernel.Add(
            new HtmlKernel());

        compositeKernel.Add(
            new KeyValueStoreKernel()
                .UseWho());

        compositeKernel.Add(
            new MermaidKernel());

        compositeKernel.Add(
            new HttpKernel()
                .UseValueSharing());

        var secretManager = new SecretManager(powerShellKernel);

        var kernel = compositeKernel
                     .UseDefaultMagicCommands()
                     .UseAboutMagicCommand()
                     .UseImportMagicCommand()
                     .UseSecretManager(secretManager)
                     .UseFormsForMultipleInputs(secretManager)
                     .UseNuGetExtensions(telemetrySender)
                     .UseCodeExpansions(() =>
                     {
                         // FIX: (CreateKernel) refactor this out

                         RecentConnectionList recentlyConnections;

                         if (secretManager.TryGetValue("dotnet-interactive.RecentlyUsedConnections", out var json))
                         {
                             recentlyConnections = JsonSerializer.Deserialize<RecentConnectionList>(json, Serializer.JsonSerializerOptions);
                         }
                         else
                         {
                             recentlyConnections = new();
                         }

                         return recentlyConnections;
                     }, list =>
                     {
                         var json = JsonSerializer.Serialize(list, Serializer.JsonSerializerOptions);
                         secretManager.SetValue("dotnet-interactive.RecentlyUsedConnections", json);
                     });

        kernel.AddConnectDirective(new ConnectSignalRDirective());
        kernel.AddConnectDirective(new ConnectStdIoDirective(startupOptions.KernelHost));

        kernel.AddConnectDirective(
            new ConnectJupyterKernelDirective()
                .AddConnectionOptions(new JupyterHttpKernelConnectionOptions())
                .AddConnectionOptions(new JupyterLocalKernelConnectionOptions()));

        SetUpFormatters(frontendEnvironment);

        kernel.DefaultKernelName = defaultKernelName;

        kernel.UseTelemetrySender(telemetrySender);

        return kernel;
    }

    internal static void SetUpFormatters(FrontendEnvironment frontendEnvironment)
    {
        if (frontendEnvironment is BrowserFrontendEnvironment)
        {
            Formatter.DefaultMimeType = HtmlFormatter.MimeType;
            Formatter.SetPreferredMimeTypesFor(typeof(string), PlainTextFormatter.MimeType);
        }
    }
}