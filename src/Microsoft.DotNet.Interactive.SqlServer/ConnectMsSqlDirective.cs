// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.SqlServer;

public class ConnectMsSqlDirective : ConnectKernelDirective<ConnectMsSqlKernel>
{
    private readonly string ResolvedToolsServicePath;

    public ConnectMsSqlDirective(string resolvedToolsServicePath)
        : base("mssql", "Connects to a Microsoft SQL Server database")
    {
        ResolvedToolsServicePath = resolvedToolsServicePath;
        Parameters.Add(ConnectionStringParameter);
        Parameters.Add(CreateDbContextParameter);
    }

    private static KernelDirectiveParameter CreateDbContextParameter { get; } =
        new("--create-dbcontext",
            "Scaffold a DbContext in the C# kernel.")
        {
            Flag = true
        };

    private KernelDirectiveParameter ConnectionStringParameter { get; } =
        new("--connection-string", description: "The connection string used to connect to the database")
        {
            AllowImplicitName = true,
            Required = true,
            TypeHint = "connectionstring-mssql"
        };

    public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        ConnectMsSqlKernel connectCommand,
        KernelInvocationContext context)
    {
        var connector = new MsSqlKernelConnector(
            connectCommand.CreateDbContext,
            connectCommand.ConnectionString);

        connector.PathToService = ResolvedToolsServicePath;

        var localName = connectCommand.ConnectedKernelName;

        var found = context.HandlingKernel?.RootKernel.FindKernelByName($"sql-{localName}") is not null;

        if (found)
        {
            throw new InvalidOperationException(
                $"A kernel with name {localName} is already present. Use a different value for the {KernelNameParameter.Name} parameter.");
        }

        var kernel = await connector.CreateKernelAsync(localName);

        if (connector.CreateDbContext)
        {
            await InitializeDbContextAsync(localName, connector, context);
        }

        return new[] { kernel };
    }

    private async Task InitializeDbContextAsync(string kernelName, MsSqlKernelConnector options, KernelInvocationContext context)
    {
        CSharpKernel csharpKernel = null;

        context.HandlingKernel.RootKernel.VisitSubkernelsAndSelf(k =>
        {
            if (k is CSharpKernel csk)
            {
                csharpKernel = csk;
            }
        });

        if (csharpKernel is null)
        {
            return;
        }

        context.DisplayAs($"Scaffolding a `DbContext` and initializing an instance of it called `{kernelName}` in the C# kernel.", "text/markdown");

        // FIX: (InitializeDbContextAsync) package versions to make them reference the ones that are already referenced at build time
        var submission1 = $$"""
            #r "nuget: Microsoft.Data.SqlClient, 5.2.2"
            #r "nuget: Microsoft.EntityFrameworkCore.Design, 8.0.8"
            #r "nuget: Microsoft.EntityFrameworkCore.SqlServer, 8.0.8"
            #r "nuget: Humanizer.Core, 2.14.1"
            #r "nuget: Humanizer, 2.14.1"
            #r "nuget: Microsoft.Identity.Client, 4.61.3"
            
            using System;
            using System.Reflection;
            using System.Linq;
            using Microsoft.EntityFrameworkCore.Design;
            using Microsoft.EntityFrameworkCore.Scaffolding;
            using Microsoft.Extensions.DependencyInjection;

            var services = new ServiceCollection();
            services.AddEntityFrameworkDesignTimeServices();
            var providerAssembly = Assembly.Load("Microsoft.EntityFrameworkCore.SqlServer");
            var providerServicesAttribute = providerAssembly.GetCustomAttribute<DesignTimeProviderServicesAttribute>();
            var providerServicesType = providerAssembly.GetType(providerServicesAttribute.TypeName);
            var providerServices = (IDesignTimeServices)Activator.CreateInstance(providerServicesType);
            providerServices.ConfigureDesignTimeServices(services);

            var serviceProvider = services.BuildServiceProvider();
            var scaffolder = serviceProvider.GetService<IReverseEngineerScaffolder>();

            var model = scaffolder.ScaffoldModel(
                @"{{options.ConnectionString}}",
                new DatabaseModelFactoryOptions(),
                new ModelReverseEngineerOptions(),
                new ModelCodeGenerationOptions()
                {
                    ContextName = "{{kernelName}}Context",
                    ModelNamespace = "{{kernelName}}"
                });

            var code = @"using System;
            using System.Collections.Generic;
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata;";

            foreach (var file in  new[] { model.ContextFile.Code }.Concat(model.AdditionalFiles.Select(f => f.Code)))
            {
                var namespaceToFind = "namespace {{kernelName}};";
                var headerSize = file.LastIndexOf(namespaceToFind)  + namespaceToFind.Length;
                var fileCode = file
                    // remove namespaces, which don't compile in Roslyn scripting
                    .Substring(headerSize).Trim();
            
                code += fileCode;
            }

            """;

        var submitCode = new SubmitCode(submission1);
        submitCode.SetParent(context.Command);
        await csharpKernel.SendAsync(submitCode, context.CancellationToken);

        csharpKernel.TryGetValue("code", out string submission2);
        await csharpKernel.SubmitCodeAsync(submission2);

        var submission3 = $@"
var {kernelName} = new {kernelName}Context();";

        await csharpKernel.SubmitCodeAsync(submission3);
    }
}