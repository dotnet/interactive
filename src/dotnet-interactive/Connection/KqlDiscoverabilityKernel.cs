// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.App.Connection;

/// <remarks>This kernel is used as a placeholder for the MSKQL kernel in order to enable KQL language coloring in the editor. Language grammars can only be defined for fixed kernel names, but MSKQL subkernels are user-defined via the #!connect magic command. So, this kernel is specified in addition to the user-defined kernel as a kind of "styling" kernel as well as to provide guidance and discoverability for KQL features. </remarks>
public class KqlDiscoverabilityKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>
{
    private readonly HashSet<string> _kernelNameFilter;
    private const string DefaultKernelName = "kql";

    public KqlDiscoverabilityKernel() : base(DefaultKernelName)
    {
        _kernelNameFilter = new HashSet<string>
        {
            "MsKqlKernel"
        };
        KernelInfo.LanguageName = "KQL";
        KernelInfo.Description = """
                            Query a Kusto database
                            """;
    }

    Task IKernelCommandHandler<SubmitCode>.HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        var root = (Kernel)ParentKernel ?? this;

        var connectedKqlKernelNames = new HashSet<string>();

        root.VisitSubkernels(childKernel =>
        {
            if (_kernelNameFilter.Contains(childKernel.GetType().Name))
            {
                connectedKqlKernelNames.Add(childKernel.Name);
            }
        });

        var codeSample = !string.IsNullOrWhiteSpace(command.Code)
                             ? command.Code
                             : "tableName | take 10";

        if (connectedKqlKernelNames.Count is 0)
        {
            var version = PackageAcquisition.InferCompatiblePackageVersion();

            context.Display(
                HTML(
                    $"""
                     <p>A KQL connection has not been established.</p>
                     <p>To connect to a database, first add the KQL extension package by running the following in a C# cell:</p>
                     <code>
                         <pre>
                         #r "nuget:Microsoft.DotNet.Interactive.Kql,{version}"
                         </pre>
                     </code>
                     Now, you can connect to a Microsoft Kusto Server database by running the following in a C# cell:
                     <code>
                         <pre>
                         #!connect kql --kernel-name mydatabase --cluster "https://help.kusto.windows.net" --database "Samples"
                         </pre>
                     </code>
                     <p>Once a connection is established, you can send KQL statements by prefixing them with the magic command for your connection.</p>
                     <code>
                         <pre>
                     #!kql-mydatabase
                     {codeSample}
                         </pre>
                     </code>

                     """), "text/html");
        }
        else
        {
            PocketView view =
                div(
                    p("You can send KQL statements to one of the following connected KQL kernels:"),
                    connectedKqlKernelNames.Select(
                        name =>
                            code(
                                pre($"#!{name}\n{codeSample}"))));

            context.Display(view);
        }

        if (!string.IsNullOrWhiteSpace(command.Code))
        {
            context.Fail(command, message: "KQL statements cannot be executed in this kernel.");
        }

        return Task.CompletedTask;
    }
}