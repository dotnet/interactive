// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class MsSqlKernelConnection : ConnectKernelCommand<MsSqlConnectionOptions>
    {
        public MsSqlKernelConnection()
            : base("mssql", "Connects to a Microsoft SQL Server database")
        {
            Add(new Argument<string>("connectionString", "The connection string used to connect to the database"));
            Add(new Option<bool>("--create-dbcontext", "Scaffold a DbContext in the C# kernel."));
        }

        public override async Task<Kernel> CreateKernelAsync(
            MsSqlConnectionOptions options,
            KernelInvocationContext context)
        {
            var resolvedPackageReferences = ((ISupportNuget)context.HandlingKernel).ResolvedPackageReferences;
            // Walk through the packages looking for the package that endswith the name "Microsoft.SqlToolsService"
            // and grab the packageroot
            var runtimePackageIdSuffix = "Microsoft.SqlToolsService";
            var root = resolvedPackageReferences.FirstOrDefault(p => p.PackageName.EndsWith(runtimePackageIdSuffix, StringComparison.OrdinalIgnoreCase));
            string pathToService = "";
            if (root != null)
            {
                // Extract the platform 'osx-x64' from the package name 'runtime.osx-x64.native.microsoft.sqltoolsservice'
                string[] packageNameSegments = root.PackageName.Split(".");
                if (packageNameSegments.Length > 2)
                {
                    string platform = packageNameSegments[1];
                    
                    // Build the path to the MicrosoftSqlToolsServiceLayer executable by reaching into the resolve nuget package
                    // assuming a convention for native binaries.
                    pathToService = Path.Combine(
                        root.PackageRoot,
                        "runtimes",
                        platform,
                        "native",
                        "MicrosoftSqlToolsServiceLayer");
                    
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        pathToService += ".exe";
                    }
                }
            }
            
            var kernel = new MsSqlKernel(
                pathToService,
                options.KernelName,
                options.ConnectionString);

            await kernel.ConnectAsync();

            if (options.CreateDbContext)
            {
                await InitializeDbContextAsync(options, context);
            }

            return kernel;
        }
        
        private async Task InitializeDbContextAsync(MsSqlConnectionOptions options, KernelInvocationContext context)
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

            context.Display($"Scaffolding a `DbContext` and initializing an instance of it called `{options.KernelName}` in the C# kernel.", "text/markdown");

            var submission1 = @$"
#r ""nuget:Microsoft.EntityFrameworkCore.Design,5.0.0""
#r ""nuget:Microsoft.EntityFrameworkCore.SqlServer,5.0.0""

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
        ContextName = ""{options.KernelName}Context"",
        ModelNamespace = ""{options.KernelName}""
    }});

var code = @""using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;"";

foreach (var file in  new[] {{ model.ContextFile.Code }}.Concat(model.AdditionalFiles.Select(f => f.Code)))
{{
    var fileCode = file
        // remove namespaces, which don't compile in Roslyn scripting
        .Replace(""namespace {options.KernelName}"", """")

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

            csharpKernel.TryGetVariable("code", out string submission2);

            await csharpKernel.SubmitCodeAsync(submission2);

            var submission3 = $@"
var {options.KernelName} = new {options.KernelName}Context();";

            await csharpKernel.SubmitCodeAsync(submission3);
        }
    }

    public class MsSqlConnectionOptions : SqlConnectionOptions
    {
        public bool CreateDbContext { get; set; }
    }
}
