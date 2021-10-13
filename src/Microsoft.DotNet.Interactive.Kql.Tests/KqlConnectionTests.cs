using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.ExtensionLab;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Kql.Tests
{
    public class KqlConnectionTests : IDisposable
    {
        private static async Task<CompositeKernel> CreateKernel()
        {
            var csharpKernel = new CSharpKernel().UseNugetDirective();
            await csharpKernel.SubmitCodeAsync(@$"
#r ""nuget:microsoft.sqltoolsservice,3.0.0-release.53""
");

            // TODO: remove KQLKernel it is used to test current patch
            var kernel = new CompositeKernel
            {
                new KqlDiscoverabilityKernel(),
                csharpKernel,
                new KeyValueStoreKernel()
            };

            kernel.DefaultKernelName = csharpKernel.Name;

            kernel.UseKernelClientConnection(new ConnectKqlCommand());
            kernel.UseNteractDataExplorer();

            return kernel;
        }

        [KqlFact]
        public async Task It_can_connect_and_query_data()
        {
            var cluster = KqlFactAttribute.GetClusterForTests();
            using var kernel = await CreateKernel();
            var result = await kernel.SubmitCodeAsync(
                $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

            result.KernelEvents
                .ToSubscribedList()
                .Should()
                .NotContainErrors();

            result = await kernel.SubmitCodeAsync(@"
#!kql-KustoHelp
StormEvents | take 10
            ");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                .NotContainErrors()
                .And
                .ContainSingle<DisplayedValueProduced>(e =>
                    e.FormattedValues.Any(f => f.MimeType == PlainTextFormatter.MimeType));

            events.Should()
                .ContainSingle<DisplayedValueProduced>(e =>
                    e.FormattedValues.Any(f => f.MimeType == HtmlFormatter.MimeType));
        }

        [KqlFact]
        public async Task sending_query_to_kusto_will_generate_suggestions()
        {
            var cluster = KqlFactAttribute.GetClusterForTests();
            using var kernel = await CreateKernel();
            var result = await kernel.SubmitCodeAsync(
                $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

            result.KernelEvents
                .ToSubscribedList()
                .Should()
                .NotContainErrors();

            result = await kernel.SubmitCodeAsync(@"
#!kql
StormEvents | take 10
");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                .NotContainErrors()
                .And
                .ContainSingle<DisplayedValueProduced>(e =>
                    e.FormattedValues.Any(f => f.MimeType == HtmlFormatter.MimeType))
                .Which.FormattedValues.Single(f => f.MimeType == HtmlFormatter.MimeType)
                .Value
                .Should()
                .Contain("#!kql-KustoHelp")
                .And
                .Contain(" tableName | take 10");

        }

        [KqlFact]
        public async Task Field_types_are_deserialized_correctly()
        {
            var cluster = KqlFactAttribute.GetClusterForTests();
            using var kernel = await CreateKernel();
            var result = await kernel.SubmitCodeAsync(
                $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .NotContainErrors();

            result = await kernel.SubmitCodeAsync($@"
#!kql-KustoHelp --mime-type {TabularDataResourceFormatter.MimeType}
StormEvents | take 10
");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            var value = events.Should()
                    .ContainSingle<DisplayedValueProduced>(e =>
                        e.FormattedValues.Any(f => f.MimeType == HtmlFormatter.MimeType))
                              .Which;

            var table = (NteractDataExplorer)value.Value;

            table.Data
                 .Schema
                 .Fields
                 .Should()
                 .ContainSingle(f => f.Name == "StartTime")
                 .Which
                 .Type
                 .Should()
                 .Be(TableSchemaFieldType.DateTime);
        }

        [KqlFact]
        public async Task Empty_results_are_displayed_correctly()
        {
            var cluster = KqlFactAttribute.GetClusterForTests();
            using var kernel = await CreateKernel();
            var result = await kernel.SubmitCodeAsync(
                $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .NotContainErrors();

            result = await kernel.SubmitCodeAsync($@"
#!kql-KustoHelp --mime-type {TabularDataResourceFormatter.MimeType}
StormEvents | take 0
");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                .NotContainErrors()
                .And
                    .ContainSingle<DisplayedValueProduced>(e =>
                        e.FormattedValues.Any(f => f.MimeType == PlainTextFormatter.MimeType && f.Value.ToString().StartsWith("Info")));
        }

        [KqlTheory]
        [InlineData("var testVar = 2;", (long)2)] // var
        [InlineData("var testVar = \"hi!\";", "hi!")] // var string
        [InlineData("string testVar = \"hi!\";", "hi!")] // string
        [InlineData("string testVar = \"«ταБЬℓσ»\";", "«ταБЬℓσ»")] // unicode
        [InlineData("string testVar = \"\";", "")] // Empty string
        [InlineData("double testVar = 123456.789;", 123456.789)] // double
        [InlineData("decimal testVar = 123456.789M;", 123456.789)] // decimal
        [InlineData("bool testVar = false;", (sbyte)0)] // bool
        [InlineData("char testVar = 'a';", "a")] // char
        [InlineData("char testVar = '\\'';", "'")] // ' char
        [InlineData("byte testVar = 123;", (long)123)] // byte
        [InlineData("int testVar = 123456;", (long)123456)] // int
        [InlineData("long testVar = 123456789012345;", 123456789012345)] // long
        [InlineData("short testVar = 123;", (long)123)] // short
        [InlineData("sbyte testVar = 123;", (long)123)] // sbyte
        [InlineData("uint testVar = 123456;", (long)123456)] // uint
        [InlineData("ulong testVar = 123456789012345;", 123456789012345)] // ulong
        [InlineData("ushort testVar = 123;", (long)123)] // ushort
        public async Task Shared_variable_can_be_used_to_parameterize_a_kql_query(string csharpVariableDeclaration, object expectedValue)
        {
            var cluster = KqlFactAttribute.GetClusterForTests();
            using var kernel = await CreateKernel();
            var result = await kernel.SubmitCodeAsync(
                $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

            result.KernelEvents
                .ToSubscribedList()
                .Should()
                .NotContainErrors();

            await kernel.SendAsync(new SubmitCode(csharpVariableDeclaration));

            var code = @"
#!kql-KustoHelp
#!share --from csharp testVar
print testVar";

            result = await kernel.SendAsync(new SubmitCode(code));

            var events = result.KernelEvents.ToSubscribedList();

            events
                .Should()
                .NotContainErrors();

            var data = TestUtility.GetTabularData(events);

            data.Data
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .ContainValue(expectedValue);
        }

        [KqlTheory]
        [InlineData("string testVar = null;")] // Don't support null vars currently
        [InlineData("decimal testVar = 123456.789;", true)] // Incorrect type
        [InlineData("nint testVar = 123456;")] // Unsupported type
        [InlineData("nuint testVar = 123456;")] // Unsupported type
        [InlineData("var testVar = new List<int>();")] // Unsupported type
        [InlineData("string testVar = \"tricky\\\"string\";", false, false)] // string with ", bug https://github.com/microsoft/sqltoolsservice/issues/1271
        public async Task Invalid_shared_variables_are_handled_correctly(string csharpVariableDeclaration, bool isCSharpError = false, bool expectInvalidOperationException = true)
        {
            var cluster = KqlFactAttribute.GetClusterForTests();
            using var kernel = await CreateKernel();
            var result = await kernel.SubmitCodeAsync(
                $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

            result.KernelEvents
                .ToSubscribedList()
                .Should()
                .NotContainErrors();

            var cSharpResult = await kernel.SendAsync(new SubmitCode(csharpVariableDeclaration));

            var cSharpEvents = cSharpResult.KernelEvents.ToSubscribedList();
            if (isCSharpError)
            {
                cSharpEvents
                    .Should()
                    .ContainSingle<CommandFailed>();
            }


            var code = @"
#!kql-KustoHelp
#!share --from csharp testVar
print testVar";

            result = await kernel.SendAsync(new SubmitCode(code));

            var events = result.KernelEvents.ToSubscribedList();

            var assertion = events
                .Should()
                .ContainSingle<CommandFailed>();
            Type t = typeof(StreamJsonRpc.RemoteInvocationException);
            if (!isCSharpError && expectInvalidOperationException)
            {
                // Errors that occurred in the csharp block will result in this failing, but not with an inner exception
                assertion
                    .Which
                    .Exception
                    .Should()
                    .BeOfType<InvalidOperationException>();
            }
        }

        public void Dispose()
        {
            Formatter.ResetToDefault();
        }
    }
}