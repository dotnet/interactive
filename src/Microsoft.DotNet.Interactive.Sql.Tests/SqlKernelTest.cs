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

namespace Microsoft.DotNet.Interactive.Sql.Tests
{
    public class SqlKernelTests : LanguageKernelTestBase
    {
        public SqlKernelTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(@"SELECT 'HELLO WORLD'", typeof(string))]
        public async Task events_should_contain_hello_world(string code, Type expectedType)
        {
            using var kernel = new SqlKernel();
            using var events = kernel.KernelEvents.ToSubscribedList();

            // Set connection context

            await kernel.SubmitCodeAsync(code);

            events.Should().ContainSingle<DisplayedValueProduced>().Which.Value.Should().BeOfType<string>().Which.Should().Be("HELLO WORLD");
        }
    }
}