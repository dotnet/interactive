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
            var testConnStr = "Server=(localdb)\\MSSQLLocalDB;Database=master;Integrated Security=true;";
            using var kernel = new MsSqlKernel("sql", testConnStr);

            var connectionUri = "connection:providerName:MSSQL|applicationName:dotnetTest|authenticationType:Integrated|server:(localdb)\\MSSQLLocalDB|group:286A0A8F-95DB-492C-96A2-DC1EFE7637AC";
            await kernel.ConnectAsync(connectionUri);

            var completionItemsResult = await kernel.ProvideCompletionItemsAsync();
            Assert.True(completionItemsResult != null, "Completion list should not be null");

            await kernel.DisconnectAsync(connectionUri);
        }

        [Fact]
        public async Task ConnectionTestAsync()
        {
            var testConnStr = "Server=(localdb)\\MSSQLLocalDB;Database=master;Integrated Security=true;";
            using var kernel = new MsSqlKernel("sql", testConnStr);

            var connectionUri = "connection:providerName:MSSQL|applicationName:dotnetTest|authenticationType:Integrated|server:(localdb)\\MSSQLLocalDB|group:286A0A8F-95DB-492C-96A2-DC1EFE7637AC";
            var testQuery = "SELECT 'HELLO WORLD'";
            await kernel.ConnectAsync(connectionUri);

            var queryUri = "untitled:ConnectionTestQuery";
            var queryResult = await kernel.ExecuteQueryStringAsync(queryUri, testQuery);
            Assert.True(queryResult != null, "Query result should not be null");

            var subsetResults = await kernel.ExecuteQueryExecuteSubsetAsync(queryUri);
            Assert.True(subsetResults.ResultSubset.RowCount == 1, "Row count should not be 0");
            Assert.True(subsetResults.ResultSubset.Rows[0][0].DisplayValue == "HELLO WORLD", "Display value does not match");

            await kernel.DisconnectAsync(connectionUri);
        }
    }
}