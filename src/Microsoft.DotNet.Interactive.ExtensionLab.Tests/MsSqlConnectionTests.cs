﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public class MsSqlConnectionTests
    {
        private readonly ITestOutputHelper _output;

        public MsSqlConnectionTests(ITestOutputHelper output)
        {
            _output = output;
            // FIX: (MsSqlConnectionTests) 
            Environment.SetEnvironmentVariable(
                MsSqlServiceClient.SqlToolsServiceEnvironmentVariableName,
                @"C:\temp\Microsoft.SqlToolsService-win-x64-netcoreapp3.1\MicrosoftSqlToolsServiceLayer.exe");
        }

        [MsSqlFact("Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost")]
        public async Task It_can_connect_and_query_data()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(),
                new KeyValueStoreKernel()
            };

            kernel.UseKernelClientConnection(new MsSqlKernelConnection());

            var connectionString = "Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost";

            var result = await kernel.SubmitCodeAsync(
                             $"#!connect --kernel-name adventureworks mssql \"{connectionString}\"");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .NotContainErrors();

            result = await kernel.SubmitCodeAsync(@"
#!adventureworks
SELECT TOP 100 * FROM Person.Person
");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            events.Should()
                  .ContainSingle<DisplayedValueProduced>()
                  .Which
                  .FormattedValues
                  .Should()
                  .ContainSingle(f => f.MimeType == HtmlFormatter.MimeType);
        }

        [MsSqlFact("Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost")]
        public async Task It_can_scaffold_a_DbContext_in_a_CSharpKernel()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(),
                new KeyValueStoreKernel()
            };

            kernel.UseKernelClientConnection(new MsSqlKernelConnection());

            var connectionString = "Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost";

            var result = await kernel.SubmitCodeAsync(
                             $"#!connect --kernel-name adventureworks mssql \"{connectionString}\" --create-dbcontext");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            result = await kernel.SubmitCodeAsync("adventureworks.AddressType.Count()");

            events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            events.Should()
                  .ContainSingle<ReturnValueProduced>()
                  .Which
                  .Value
                  .As<int>()
                  .Should()
                  .Be(6);
        }
    }
}