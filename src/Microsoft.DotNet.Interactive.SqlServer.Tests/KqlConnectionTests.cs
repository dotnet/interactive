using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.ExtensionLab;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.SqlServer.Tests
{
    public class KqlConnectionTests : IDisposable
    {
        private async Task<CompositeKernel> CreateKernel()
        {
            var csharpKernel = new CSharpKernel().UseNugetDirective();
            await csharpKernel.SubmitCodeAsync(@$"
#r ""nuget:microsoft.sqltoolsservice,3.0.0-release.53""
");

            // TODO: remove KQLKernel it is used to test current patch
            var kernel = new CompositeKernel
            {
                new KQLKernel(),
                csharpKernel,
                new KeyValueStoreKernel()
            };

            kernel.DefaultKernelName = csharpKernel.Name;
           
            kernel.UseKernelClientConnection(new KqlKernelConnection());
            kernel.UseNteractDataExplorer();
            kernel.UseSandDanceExplorer();

            return kernel;
        }
        
        [KqlFact]
        public async Task It_can_connect_and_query_data()
        {
            var cluster = KqlFact.GetClusterForTests();
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
            var cluster = KqlFact.GetClusterForTests();
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
            var cluster = KqlFact.GetClusterForTests();
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

            var table = (NteractDataExplorer) value.Value;

            table.Data
                 .Schema
                 .Fields
                 .Should()
                 .ContainSingle(f => f.Name == "StartTime")
                 .Which
                 .Type
                 .Should()
                 .Be(TableSchemaFieldType.String);
        }

        [KqlFact]
        public async Task Empty_results_are_displayed_correctly()
        {
            var cluster = KqlFact.GetClusterForTests();
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
        
        public void Dispose()
        {
            Formatter.ResetToDefault();
        }
    }
}