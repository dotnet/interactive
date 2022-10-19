// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    public class ConnectMsSqlCommand : ConnectKernelCommand
    {
        private readonly string ResolvedToolsServicePath;

        public ConnectMsSqlCommand(string resolvedToolsServicePath)
            : base("mssql", "Connects to a Microsoft SQL Server database")
        {
            ResolvedToolsServicePath = resolvedToolsServicePath;
            Add(ConnectionStringArgument);
            Add(CreateDbContextOption);
        }

        private static Option<bool> CreateDbContextOption { get; } =
            new("--create-dbcontext",
                "Scaffold a DbContext in the C# kernel.");

        private Argument<MsSqlConnectionString> ConnectionStringArgument { get; } =
            new("connectionString",
                description: "The connection string used to connect to the database",
                parse: s => new(s.Tokens.Single().Value));

        public override async Task<Kernel> ConnectKernelAsync(
            KernelInvocationContext context,
            InvocationContext commandLineContext)
        {
            var connector = new MsSqlKernelConnector(
                commandLineContext.ParseResult.GetValueForOption(CreateDbContextOption),
                commandLineContext.ParseResult.GetValueForArgument(ConnectionStringArgument).Value);
            connector.PathToService = ResolvedToolsServicePath;

            var localName = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);

            var kernel = await connector.CreateKernelAsync(localName);

            if (connector.CreateDbContext)
            {
                await InitializeDbContextAsync(localName, connector, context);
            }

            return kernel;
        }

        private async Task InitializeDbContextAsync(string kernelName, MsSqlKernelConnector options, KernelInvocationContext context)
        {
            CSharpKernel csharpKernel = null;

            context.HandlingKernel.VisitSubkernelsAndSelf(k =>
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

            var submission1 = @$"  
#r ""nuget: Microsoft.EntityFrameworkCore.Design, 6.0.10""
#r ""nuget: Microsoft.EntityFrameworkCore.SqlServer, 6.0.10""
#r ""nuget: Humanizer.Core, 2.8.26""
#r ""nuget: Humanizer, 2.8.26""
#r ""nuget: Microsoft.Identity.Client, 4.35.1""

            using System;
using System.Reflection;
using System.Linq;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddEntityFrameworkDesignTimeServices();
var providerAssembly = Assembly.Load(""Microsoft.EntityFrameworkCore.SqlServer"");
var providerServicesAttribute = providerAssembly.GetCustomAttribute<DesignTimeProviderServicesAttribute>();
var providerServicesType = providerAssembly.GetType(providerServicesAttribute.TypeName);
var providerServices = (IDesignTimeServices)Activator.CreateInstance(providerServicesType);
providerServices.ConfigureDesignTimeServices(services);

var serviceProvider = services.BuildServiceProvider();
var scaffolder = serviceProvider.GetService<IReverseEngineerScaffolder>();

var model = scaffolder.ScaffoldModel(
    @""{options.ConnectionString}"",
    new DatabaseModelFactoryOptions(),
    new ModelReverseEngineerOptions(),
    new ModelCodeGenerationOptions()
    {{
        ContextName = ""{kernelName}Context"",
        ModelNamespace = ""{kernelName}""
    }});

var code = @""using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;"";

foreach (var file in  new[] {{ model.ContextFile.Code }}.Concat(model.AdditionalFiles.Select(f => f.Code)))
{{
    var fileCode = file
        // remove namespaces, which don't compile in Roslyn scripting
        .Replace(""namespace {kernelName}"", """")

        // remove the namespaces, which have been hoisted to the top of the code submission
        .Replace(""using System;"", """")
        .Replace(""using System.Collections.Generic;"", """")
        .Replace(""using Microsoft.EntityFrameworkCore;"", """")
        .Replace(""using Microsoft.EntityFrameworkCore.Metadata;"", """")

        // trim out the wrapping braces
        .Trim()
        .Trim( new[] {{ '{{', '}}' }} );

    code += fileCode;
}}
";

            await csharpKernel.SubmitCodeAsync(submission1);

            csharpKernel.TryGetValue("code", out string submission2);

            await csharpKernel.SubmitCodeAsync(submission2);

            var submission3 = $@"
var {kernelName} = new {kernelName}Context();";

            await csharpKernel.SubmitCodeAsync(submission3);
        }
    }
}
