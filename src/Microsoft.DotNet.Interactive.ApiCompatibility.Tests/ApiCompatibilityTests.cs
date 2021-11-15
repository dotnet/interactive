﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Assent;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Journey;
using Microsoft.DotNet.Interactive.Kql;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.SqlServer;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.ApiCompatibility.Tests;

public class ApiCompatibilityTests
{
    private readonly Configuration _configuration;

    public ApiCompatibilityTests()
    {
        _configuration = new Configuration()
                         .SetInteractive(Debugger.IsAttached)
                         .UsingExtension("txt");
    }

    [FactSkipLinux]
    public void Interactive_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<Kernel>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void Formatting_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<FormatContext>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void Document_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<InteractiveDocument>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void PackageManagement_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<PackageRestoreContext>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void Journey_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<Lesson>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void csharp_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<CSharpKernel>();
        this.Assent(contract, _configuration);
    }

    [Fact(Skip = "need to use signature files")]
    public void fsharp_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<FSharpKernel>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void powershell_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<PowerShellKernel>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void mssql_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<MsSqlKernelConnector>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void kql_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<KqlKernelConnector>();
        this.Assent(contract, _configuration);
    }
}