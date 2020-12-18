// Copyright (c) .NET Foundation and contributors. All rights reserved.
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
        }

        private async Task<CompositeKernel> CreateKernel()
        {
            var csharpKernel = new CSharpKernel().UseNugetDirective();
            await csharpKernel.SubmitCodeAsync(@$"
#r ""nuget:runtime.osx-x64.native.microsoft.sqltoolsservice,3.0.0-release.52""
");
            
            var kernel = new CompositeKernel
            {
                csharpKernel,
                new KeyValueStoreKernel()
            };

            kernel.UseKernelClientConnection(new MsSqlKernelConnection());
            return kernel;
        }

        [MsSqlFact]
        public async Task It_can_connect_and_query_data()
        {
            var connectionString = MsSqlFact.GetConnectionStringForTests();
            using var kernel = await CreateKernel();
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

        [MsSqlFact]
        public async Task It_can_scaffold_a_DbContext_in_a_CSharpKernel()
        {
            var connectionString = MsSqlFact.GetConnectionStringForTests();
            
            using var kernel = await CreateKernel();
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