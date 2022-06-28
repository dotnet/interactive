// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.Csv;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;
using Xunit;

namespace Microsoft.DotNet.Interactive.Kql.Tests
{
    public class KqlConnectionTests : IDisposable
    {
        private static async Task<CompositeKernel> CreateKernel()
        {
            Formatter.SetPreferredMimeTypesFor(typeof(TabularDataResource), HtmlFormatter.MimeType, CsvFormatter.MimeType);
            var csharpKernel = new CSharpKernel().UseNugetDirective();
            await csharpKernel.SubmitCodeAsync(@"
#r ""nuget:Microsoft.SqlToolsService, 3.0.0-release.163""
");

            var kernel = new CompositeKernel
            {
                new KqlDiscoverabilityKernel(),
                csharpKernel,
                new KeyValueStoreKernel()
            };

            kernel.DefaultKernelName = csharpKernel.Name;

            var kqlKernelExtension = new KqlKernelExtension();
            await kqlKernelExtension.OnLoadAsync(kernel);

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
        public async Task It_can_store_result_set_with_a_name()
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
#!kql-KustoHelp --name my_data_result
StormEvents | take 10
            ");

            var kqlKernel = kernel.FindKernel("kql-KustoHelp") as ISupportGetValue;
            kqlKernel.TryGetValue("my_data_result", out object variable).Should().BeTrue();
            variable.Should().BeAssignableTo<IEnumerable<TabularDataResource>>();
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
#!kql-KustoHelp
StormEvents | take 10
");

            var events = result.KernelEvents.ToSubscribedList();

            events.ShouldDisplayTabularDataResourceWhich()
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
        public async Task query_produces_expected_formatted_values()
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
#!kql-KustoHelp
StormEvents | take 10
");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            events.Should()
                .ContainSingle<DisplayedValueProduced>(fvp => fvp.Value is DataExplorer<TabularDataResource>)
                .Which
                .FormattedValues.Select(fv => fv.MimeType)
                .Should()
                .BeEquivalentTo(HtmlFormatter.MimeType, CsvFormatter.MimeType);
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
#!kql-KustoHelp
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
                .ShouldDisplayTabularDataResourceWhich()
                .Data
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .ContainValue(expectedValue);
        }

        [KqlTheory]
        [InlineData("string testVar = null;")] // Don't support null vars currently
        [InlineData("nint testVar = 123456;")] // Unsupported type
        [InlineData("nuint testVar = 123456;")] // Unsupported type
        [InlineData("var testVar = new List<int>();")] // Unsupported type
        [InlineData("string testVar = \"tricky\\\"string\";")] // string with ", bug https://github.com/microsoft/sqltoolsservice/issues/1271
        public async Task Invalid_shared_variables_are_handled_correctly(string csharpVariableDeclaration)
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

            events.Should().ContainSingle<CommandFailed>();
        }

        public void Dispose()
        {
            Formatter.ResetToDefault();
            DataExplorer.ResetToDefault();
        }
    }
}