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
            // Walk through the packages looking for the package that endswith the name "runtime.native.Microsoft.SqlToolsService"
            // and grab the packageroot
            var runtimePackageId = "runtime.native.Microsoft.SqlToolsService";
            var runtimePackageIdSuffix = "Microsoft.SqlToolsService";
            var root = resolvedPackageReferences.FirstOrDefault(p => p.PackageName.EndsWith(runtimePackageId, StringComparison.OrdinalIgnoreCase));
            string pathToService = "";
            if (root != null)
            {
                // Packagename is rubbish, but can be reformatted to compute the path to the binaries
                var runtimePackageIdPath = root.PackageName.Replace(runtimePackageIdSuffix, "", StringComparison.OrdinalIgnoreCase)
                                                           .Replace(".", "\\", StringComparison.OrdinalIgnoreCase)
                                                           .Replace(@"\runtime\", "\\", StringComparison.OrdinalIgnoreCase)
                                                           .Replace(@"runtime\", @"runtimes\", StringComparison.OrdinalIgnoreCase);
                pathToService = Path.Combine(root.PackageRoot, runtimePackageIdPath, "MicrosoftSqlToolsServiceLayer.exe");
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
#r ""nuget:Microsoft.EntityFrameworkCore.Design,3.1.8""
#r ""nuget:Microsoft.EntityFrameworkCore.SqlServer,3.1.8""

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
