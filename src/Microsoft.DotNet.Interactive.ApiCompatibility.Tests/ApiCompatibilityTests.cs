// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Assent;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.CSharpProject;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.DuckDB;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Journey;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Kql;
using Microsoft.DotNet.Interactive.Mermaid;
using Microsoft.DotNet.Interactive.PackageManagement;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.SQLite;
using Microsoft.DotNet.Interactive.SqlServer;

namespace Microsoft.DotNet.Interactive.ApiCompatibility.Tests;

[TestProperty("Category", "Contracts and serialization")]
[TestClass]
public class ApiCompatibilityTests
{
    private readonly Configuration _configuration;

    public ApiCompatibilityTests()
    {
        _configuration = new Configuration()
                         .SetInteractive(Debugger.IsAttached)
                         .UsingExtension("txt");
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Testing api contract changes, not needed on Linux too
    [TestMethod]
    public void Interactive_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<Kernel>();
        this.Assent(contract, _configuration);
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Testing api contract changes, not needed on Linux too
    [TestMethod]
    public void Formatting_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<FormatContext>();
        this.Assent(contract, _configuration);
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Testing api contract changes, not needed on Linux too
    [TestMethod]
    public void Document_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<InteractiveDocument>();
        this.Assent(contract, _configuration);
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Testing api contract changes, not needed on Linux too
    [TestMethod]
    public void PackageManagement_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<PackageRestoreContext>();
        this.Assent(contract, _configuration);
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Testing api contract changes, not needed on Linux too
    [TestMethod]
    public void Journey_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<Lesson>();
        this.Assent(contract, _configuration);
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Testing api contract changes, not needed on Linux too
    [TestMethod]
    public void csharp_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<CSharpKernel>();
        this.Assent(contract, _configuration);
    }

    [TestMethod]
    [Ignore("this api is in early design stage.")]
    public void csharpProject_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<CSharpProjectKernel>();
        this.Assent(contract, _configuration);
    }

    [TestMethod]
    [Ignore("need to use signature files")]
    public void fsharp_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<FSharpKernel>();
        this.Assent(contract, _configuration);
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Testing api contract changes, not needed on Linux too
    [TestMethod]
    public void powershell_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<PowerShellKernel>();
        this.Assent(contract, _configuration);
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Testing api contract changes, not needed on Linux too
    [TestMethod]
    public void sqLite_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<SQLiteKernel>();
        this.Assent(contract, _configuration);
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Testing api contract changes, not needed on Linux too
    [TestMethod]
    public void mssql_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<MsSqlKernelExtension>();
        this.Assent(contract, _configuration);
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Testing api contract changes, not needed on Linux too
    [TestMethod]
    public void kql_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<KqlKernelExtension>();
        this.Assent(contract, _configuration);
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Testing api contract changes, not needed on Linux too
    [TestMethod]
    public void mermaid_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<MermaidKernel>();
        this.Assent(contract, _configuration);
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Testing api contract changes, not needed on Linux too
    [TestMethod]
    public void jupyter_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<ConnectionInformation>();
        this.Assent(contract, _configuration);
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Testing api contract changes, not needed on Linux too
    [TestMethod]
    public void httpRequest_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<HttpKernel>();
        this.Assent(contract, _configuration);
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Testing api contract changes, not needed on Linux too
    [TestMethod]
    public void DuckDB_api_is_not_changed()
    {
        var contract = ApiContract.GenerateContract<DuckDBKernel>();
        this.Assent(contract, _configuration);
    }
}