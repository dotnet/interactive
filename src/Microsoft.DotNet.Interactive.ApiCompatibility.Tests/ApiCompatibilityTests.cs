// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Assent;

using Xunit;
using System.Reflection;

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

    [Fact]
    public void Interactive_api_is_not_changed()
    {
        var contract = GenerateContract<Kernel>();
        this.Assent(contract, _configuration);
    }

    [Fact]
    public void Formatting_api_is_not_changed()
    {
        var contract = GenerateContract<Formatting.FormatContext>();
        this.Assent(contract, _configuration);
    }

    [Fact]
    public void Document_api_is_not_changed()
    {
        var contract = GenerateContract<Documents.InteractiveDocument>();
        this.Assent(contract, _configuration);
    }

    [Fact]
    public void PackageManagement_api_is_not_changed()
    {
        var contract = GenerateContract<PackageRestoreContext>();
        this.Assent(contract, _configuration);
    }

    [Fact]
    public void Journey_api_is_not_changed()
    {
        var contract = GenerateContract<Journey.Lesson>();
        this.Assent(contract, _configuration);
    }

    [Fact]
    public void csharp_api_is_not_changed()
    {
        var contract = GenerateContract<CSharp.CSharpKernel>();
        this.Assent(contract, _configuration);
    }

    [Fact]
    public void fsharp_api_is_not_changed()
    {
        var contract = GenerateContract<FSharp.FSharpKernel>();
        this.Assent(contract, _configuration);
    }

    [Fact]
    public void powershell_api_is_not_changed()
    {
        var contract = GenerateContract<PowerShell.PowerShellKernel>();
        this.Assent(contract, _configuration);
    }

    [Fact]
    public void mssql_api_is_not_changed()
    {
        var contract = GenerateContract<SqlServer.MsSqlKernelConnector>();
        this.Assent(contract, _configuration);
    }

    [Fact]
    public void kql_api_is_not_changed()
    {
        var contract = GenerateContract<Kql.KqlKernelConnector>();
        this.Assent(contract, _configuration);
    }

    private string GenerateContract<T>()
    {
        var contract = new StringBuilder();
        var assembly = typeof(T).Assembly;
        var types = assembly.GetExportedTypes().OrderBy(t => t.FullName).ToArray();

        var printedMethods = new HashSet<MethodInfo>();

        foreach (var type in types)
        {
            // statics
            foreach (var prop in type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly).OrderBy(c => c.Name))
            {
                if (prop.GetMethod?.IsPublic == true)
                {
                    var m = prop.GetMethod;
                    if (m is { } && printedMethods.Add(m))
                    {
                        contract.AppendLine($"{prop.DeclaringType.FullName}::{m}");
                    }
                }

                if (prop.GetSetMethod()?.IsPublic == true)
                {
                    var m = prop.GetSetMethod();
                    if (m is { } && printedMethods.Add(m))
                    {
                        contract.AppendLine($"{prop.DeclaringType.FullName}::{m}");
                    }
                }
            }

            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly).OrderBy(c => c.Name))
            {
                
                if (printedMethods.Add(method))
                {
                    contract.AppendLine($"{method.DeclaringType.FullName}::{method}");
                }
               
            }

            // instance
            foreach (var ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public| BindingFlags.DeclaredOnly).OrderBy(c => c.Name))
            {
                contract.AppendLine($"{ctor.DeclaringType.FullName}::{ctor}");
            }

            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).OrderBy(c => c.Name))
            {
                if (prop.GetMethod?.IsPublic == true)
                {
                    var m = prop.GetMethod;
                    if (m is { } && printedMethods.Add(m))
                    {
                        contract.AppendLine($"{prop.DeclaringType.FullName}::{m}");
                    }
                }

                if (prop.GetSetMethod()?.IsPublic == true)
                {
                    var m = prop.GetSetMethod();
                    if (m is { } && printedMethods.Add(m))
                    {
                        contract.AppendLine($"{prop.DeclaringType.FullName}::{m}");
                    }
                }
            }

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).OrderBy(c => c.Name))
            {
                if (printedMethods.Add(method))
                {
                    contract.AppendLine($"{method.DeclaringType.FullName}::{method}");
                }
            }
        }

        return contract.ToString();
    }
}

