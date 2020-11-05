using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.ExtensionLab;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Sql.Tests
{
    public class MsSqlKernelTests : LanguageKernelTestBase
    {
        public MsSqlKernelTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(@"SELECT 'HELLO WORLD'", typeof(string))]
        public async Task HellowWorldEventTestAsync(string code, Type expectedType)
        {
            using var kernel = new MsSqlKernel("sql", "");
            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(code);

            events.Should().ContainSingle<DisplayedValueProduced>().Which.Value.Should().BeOfType(expectedType).And.Subject.Should().Be("HELLO WORLD");
        }
    }
}