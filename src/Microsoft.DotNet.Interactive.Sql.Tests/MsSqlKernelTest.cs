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
        public async Task HellowWorldEventTest(string code, Type expectedType)
        {
            using var kernel = new MsSqlKernel("sql", "");
            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(code);

            events.Should().ContainSingle<DisplayedValueProduced>().Which.Value.Should().BeOfType(expectedType).And.Subject.Should().Be("HELLO WORLD");
        }
    }
}