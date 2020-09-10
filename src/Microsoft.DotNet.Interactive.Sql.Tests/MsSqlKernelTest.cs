using System;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;
using FluentAssertions;
using System.Linq;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using XPlot.Plotly;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.ExtensionLab;

namespace Microsoft.DotNet.Interactive.Sql.Tests
{
    public class MsSqlKernelTests : LanguageKernelTestBase
    {
        public MsSqlKernelTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(@"SELECT 'HELLO WORLD'", typeof(string))]
        public async Task events_should_contain_hello_worldAsync(string code, Type expectedType)
        {
            using var kernel = new MsSqlKernel("sql", "");
            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(code);

            events.Should().ContainSingle<DisplayedValueProduced>().Which.Value.Should().BeOfType(expectedType).And.Subject.Should().Be("HELLO WORLD");
        }
        
        [Fact]
        public async Task CompletionTestAsync()
        {
            var testConnStr = "Server=(localdb)\\MSSQLLocalDB;Database=tempdb;Integrated Security=true;";
            using var kernel = new MsSqlKernel("sql", testConnStr);
            using var events = kernel.KernelEvents.ToSubscribedList();

            var testUri = "connection://test";
            var connectResult = await kernel.ConnectAsync(testUri);
            Assert.True(connectResult, "Connect should succeed.");

            var completionItemsResult = await kernel.ProvideCompletionItemsAsync();
            Assert.True(completionItemsResult != null, "Completion list should not be null");

            var disconnectResult = await kernel.DisconnectAsync(testUri);
            Assert.True(disconnectResult, "Disconnect should succeed.");
        }
        [Fact]
        public async Task ConnectionTestAsync()
        {
            var testConnStr = "Server=(localdb)\\MSSQLLocalDB;Database=tempdb;Integrated Security=true;";
            using var kernel = new MsSqlKernel("sql", testConnStr);
            using var events = kernel.KernelEvents.ToSubscribedList();

            var testUri = "connection://test";
            var testQuery = "SELECT 'HELLO WORLD'";

            var connectResult = await kernel.ConnectAsync(testUri);
            Assert.True(connectResult, "Connection attempt should succeed");

            System.Threading.Thread.Sleep(5000);

            var queryResult = await kernel.ExecuteQueryStringAsync(testUri, testQuery);
            Assert.True(queryResult != null, "Query result should not be null");

            System.Threading.Thread.Sleep(5000);

            var subsetResults = await kernel.ExecuteQueryExecuteSubsetAsync(testUri);
            Assert.True(subsetResults.ResultSubset.RowCount == 1, "Row count should not be 0");
            Assert.True(subsetResults.ResultSubset.Rows[0][0].DisplayValue == "HELLO WORLD", "Display value does not match");

            var disconnectResult = await kernel.DisconnectAsync(testUri);
            Assert.True(disconnectResult, "Disconnect should succeed.");
        }
    }
}