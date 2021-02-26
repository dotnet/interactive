// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class SqlKernelTests
    {

        [Fact]
        public async Task sql_kernel_does_not_execute_query()
        {
            using var kernel = new CompositeKernel
            {
                new SqlKernel()
            };

            using var events = kernel.KernelEvents.ToSubscribedList();

            var query = "select * from sys.databases";
            await kernel.SendAsync(new SubmitCode($"#!sql\n\n{query}"));

            var displayValue = events.Should()
                .ContainSingle<DisplayedValueProduced>()
                .Which;

            var message = (string)displayValue.Value;

            message.Should()
                .NotContain(query);
        }

        [Fact]
        public async Task sql_kernel_emits_help_message_without_sql_server_extension_installed()
        {
            using var kernel = new CompositeKernel
            {
                new SqlKernel()
            };

            using var events = kernel.KernelEvents.ToSubscribedList();

            var query = "select * from sys.databases";
            await kernel.SendAsync(new SubmitCode($"#!sql\n\n{query}"));

            var displayValue = events.Should()
                .ContainSingle<DisplayedValueProduced>()
                .Which;

            var message = (string)displayValue.Value;
            

            // Should contain instructions for how to install SqlServer extension package
            message.Should().Contain(@"#r ""nuget:Microsoft.DotNet.Interactive.SqlServer,*-*""");

            // Should contain instructions for how to get help message for MSSQL kernel
            message.Should().Contain("#!connect mssql -h");
        }
    }
}