// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Newtonsoft.Json;
using Recipes;
using Xunit;
using Xunit.Abstractions;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Tests
{
#pragma warning disable 8509
    public class LanguageKernelPackageTests : LanguageKernelTestBase
    {
        public LanguageKernelPackageTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_can_load_assembly_references_using_r_directive_single_submission(Language language)
        {
            var kernel = CreateKernel(language);

            // F# strings treat \ as an escape character.  So do C# strings, except #r in C# is special, and doesn't.  F# usually uses @ strings for paths @"c:\temp\...."
            var dllPath = new FileInfo(typeof(JsonConvert).Assembly.Location).FullName.Replace('\\', '/');

            var source = language switch
            {
                Language.FSharp => $@"#r ""{dllPath}""
open Newtonsoft.Json
let json = JsonConvert.SerializeObject( struct {{| value = ""hello"" |}} )
json",

                Language.CSharp => $@"#r ""{dllPath}""
using Newtonsoft.Json;
var json = JsonConvert.SerializeObject(new {{ value = ""hello"" }});
json"
            };

            await SubmitCode(kernel, source);

            KernelEvents
                .Should()
                .ContainSingle(e => e is ReturnValueProduced);

            KernelEvents
                .OfType<ReturnValueProduced>()
                .Single()
                .Value
                .Should()
                .Be(new { value = "hello" }.ToJson());
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_can_load_assembly_references_using_r_directive_separate_submissions(Language language)
        {
            var kernel = CreateKernel(language);

            // F# strings treat \ as an escape character.  So do C# strings, except #r in C# is special, and doesn't.  F# usually uses @ strings for paths @"c:\temp\...."
            var dllPath = new FileInfo(typeof(JsonConvert).Assembly.Location).FullName.Replace('\\', '/');

            var source = language switch
            {
                Language.FSharp => new[]
                {
                    $"#r \"{dllPath}\"",
                    "open Newtonsoft.Json",
                    @"let json = JsonConvert.SerializeObject( struct {| value = ""hello"" |} )",
                    "json"
                },

                Language.CSharp => new[]
                {
                    $"#r \"{dllPath}\"",
                    "using Newtonsoft.Json;",
                    @"var json = JsonConvert.SerializeObject(new { value = ""hello"" });",
                    "json"
                }
            };

            await SubmitCode(kernel, source);

            KernelEvents
                .Should()
                .ContainSingle(e => e is ReturnValueProduced);

            KernelEvents
                .OfType<ReturnValueProduced>()
                .Single()
                .Value
                .Should()
                .Be(new { value = "hello" }.ToJson());
        }


        [Theory]
        [InlineData(Language.CSharp, false)]
        [InlineData(Language.FSharp, false)]
        [InlineData(Language.CSharp, true)]
        //[InlineData(Language.FSharp, true, Skip = "FSharp, relative is not relative to cwd.")]
        public async Task it_can_load_assembly_references_using_r_directive_with_relative_path(Language language, bool changeWorkingDirectory)
        {
            var workingDirectory = Directory.GetCurrentDirectory();
            DisposeAfterTest(() => Directory.SetCurrentDirectory(workingDirectory));

            var kernel = CreateKernel(language);

            if (changeWorkingDirectory)
            {
                await kernel.SendAsync(new SubmitCode("System.IO.Directory.SetCurrentDirectory(\"..\")"));
            }

            var fullName = new FileInfo(typeof(JsonConvert).Assembly.Location).FullName;

            var currentDirectoryName = new DirectoryInfo(Directory.GetCurrentDirectory()).Name;

            var relativeDllPath = Path.GetRelativePath(
                Directory.GetCurrentDirectory(),
                fullName);

            var relativePath =
                Path.Combine(
                    "..",
                    currentDirectoryName,
                    relativeDllPath)
                .Replace("\\", "/");

            var code = language switch
            {
                Language.CSharp => $"#r \"{relativePath}\"",
                Language.FSharp => $"#r \"{relativePath}\""
            };

            var command = new SubmitCode(code);

            await kernel.SendAsync(command);

            KernelEvents.Should()
                        .ContainSingle<CommandSucceeded>(c => c.Command == command);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_returns_completion_list_for_types_imported_at_runtime(Language language)
        {
            var kernel = CreateKernel(language);

            var dll = new FileInfo(typeof(JsonConvert).Assembly.Location).FullName;

            var code = language switch
            {
                Language.CSharp => $"#r \"{dll}\"",
                Language.FSharp => $"#r @\"{dll}\""
            };

            await kernel.SendAsync(new SubmitCode(code));

            await kernel.SendAsync(new RequestCompletions("Newtonsoft.Json.JsonConvert.", new LinePosition(0, 28)));

            KernelEvents.Should()
                        .ContainSingle(e => e is CompletionRequestReceived);

            KernelEvents.Single(e => e is CompletionsProduced)
                        .As<CompletionsProduced>()
                        .Completions
                        .Should()
                        .Contain(i => i.DisplayText == "SerializeObject");
        }

        [Theory]
        [InlineData(Language.CSharp, "var a = new List<int>();")]
        [InlineData(Language.FSharp, "let _ = List<int>()")]
        public async Task When_SubmitCode_command_adds_packages_to_the_kernel_then_the_submission_is_not_passed_to_the_script(Language language, string assignment)
        {
            using var kernel = CreateKernel(language);
            using var events = kernel.KernelEvents.ToSubscribedList();

            var command = new SubmitCode("#r \"nuget:Microsoft.ML, 1.3.1\"" + Environment.NewLine + assignment);
            await kernel.SendAsync(command);

            events
                .OfType<CodeSubmissionReceived>()
                .Should()
                .NotContain(e => e.Code.Contains("#r \"nuget:"));
        }

        [Theory]
        [InlineData(Language.CSharp, "Microsoft.Extensions.Logging.ILogger logger = null;")]
        [InlineData(Language.FSharp, "let logger: Microsoft.Extensions.Logging.ILogger = null")]
        public async Task When_SubmitCode_command_adds_packages_to_kernel_then_PackageAdded_event_is_raised(Language language, string expression)
        {
            using Kernel kernel = language switch
            {
                Language.CSharp => new CompositeKernel { new CSharpKernel().UseNugetDirective() },
                Language.FSharp => new CompositeKernel { new FSharpKernel().UseNugetDirective() }
            };

            var code = $@"
#r ""nuget:Microsoft.Extensions.Logging, 2.2.0""
{expression}".Trim();
            var command = new SubmitCode(code);

            var result = await kernel.SendAsync(command);

            using var events = result.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle<DisplayedValueProduced>()
                .Which
                .Value
                .As<string>()
                .Should()
                .Contain("Installing package Microsoft.Extensions.Logging");

            events
                .Should()
                .ContainSingle<DisplayedValueUpdated>(e => e.Value.Equals("Installed package Microsoft.Extensions.Logging version 2.2.0"));

            events.OfType<PackageAdded>()
                  .Should()
                  .ContainSingle(e => e.PackageReference.PackageName == "Microsoft.Extensions.Logging"
                                      && e.PackageReference.PackageVersion == "2.2.0");
        }


        [Fact]
        public async Task Loads_native_dependencies_from_nugets()
        {
            using var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            var command = new SubmitCode(@"
#r ""nuget:Microsoft.ML, 1.3.1""

using Microsoft.ML;
using Microsoft.ML.Data;
using System;

class IrisData
{
    public IrisData(float sepalLength, float sepalWidth, float petalLength, float petalWidth)
    {
        SepalLength = sepalLength;
        SepalWidth = sepalWidth;
        PetalLength = petalLength;
        PetalWidth = petalWidth;
    }
    public float SepalLength;
    public float SepalWidth;
    public float PetalLength;
    public float PetalWidth;
}

var data = new[]
{
    new IrisData(1.4f, 1.3f, 2.5f, 4.5f),
    new IrisData(2.4f, 0.3f, 9.5f, 3.4f),
    new IrisData(3.4f, 4.3f, 1.6f, 7.5f),
    new IrisData(3.9f, 5.3f, 1.5f, 6.5f),
};

MLContext mlContext = new MLContext();
var pipeline = mlContext.Transforms
    .Concatenate(""Features"", ""SepalLength"", ""SepalWidth"", ""PetalLength"", ""PetalWidth"")
    .Append(mlContext.Clustering.Trainers.KMeans(""Features"", numberOfClusters: 2));

try
{
    pipeline.Fit(mlContext.Data.LoadFromEnumerable(data));
    Console.WriteLine(""success"");
}
catch (Exception e)
{
    Console.WriteLine(e);
}");

            await kernel.SendAsync(command);

            events
                .Should()
                .Contain(e => e is PackageAdded);

            events
                .Should()
                .ContainSingle<StandardOutputValueProduced>(e => e.Value.As<string>().Contains("success"));
        }

        [Fact]
        public async Task Dependency_version_conflicts_are_resolved_correctly()
        {
            var kernel = CreateKernel(Language.CSharp);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"#!time
#i ""nuget:https://dotnet.myget.org/F/dotnet-corefxlab/api/v3/index.json""
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview""
#r ""nuget:Microsoft.Data.DataFrame,1.0.0-e190910-1""
");

            await kernel.SubmitCodeAsync(@"
using Microsoft.Data;
using XPlot.Plotly;");

            await kernel.SubmitCodeAsync(@"
using Microsoft.AspNetCore.Html;
Formatter<DataFrame>.Register((df, writer) =>
{
    var headers = new List<IHtmlContent>();
    headers.Add(th(i(""index"")));
    headers.AddRange(df.Columns.Select(c => (IHtmlContent) th(c)));
    var rows = new List<List<IHtmlContent>>();
    var take = 20;
    for (var i = 0; i < Math.Min(take, df.RowCount); i++)
    {
        var cells = new List<IHtmlContent>();
        cells.Add(td(i));
        foreach (var obj in df[i])
        {
            cells.Add(td(obj));
        }
        rows.Add(cells);
    }
    
    var t = table(
        thead(
            headers),
        tbody(
            rows.Select(
                r => tr(r))));
    
    writer.Write(t);
}, ""text/html"");");

            events.Should().NotContainErrors();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Pound_r_nuget_disallows_empty_package_specification(Language language)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"#r ""nuget:""");

            events
                .Should()
                .ContainSingle<CommandFailed>()
                .Which
                .Message
                .Should()
                .Be("Unable to parse package reference: \"nuget:\"");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Pound_r_nuget_disallows_version_only_package_specification(Language language)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
#!time
#r ""nuget:,1.0.0""
");

            events
                .Should()
                .ContainSingle<CommandFailed>()
                .Which
                .Message
                .Should()
                .Be("Unable to parse package reference: \"nuget:,1.0.0\"");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Pound_i_nuget_allows_RestoreSources_package_specification(Language language)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
#!time
#i ""nuget:https://completelyFakerestoreSource""
");

            events.Should().NotContainErrors();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Pound_i_nuget_displays_list_of_added_sources(Language language)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"#i ""nuget:https://completelyFakerestoreSource""");

            events.Should()
                  .ContainSingle<DisplayedValueProduced>()
                  .Which
                  .FormattedValues
                  .Should()
                  .ContainSingle(v => v.MimeType == "text/html")
                  .Which
                  .Value
                  .Should()
                  .ContainAll("Restore sources", "https://completelyFakerestoreSource");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Pound_i_nuget_allows_duplicate_sources_package_specification_single_cell(Language language)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
#!time
#i ""nuget:https://completelyFakerestoreSource""
#i ""nuget:https://completelyFakerestoreSource""
");

            events.Should().NotContainErrors();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Pound_i_nuget_allows_duplicate_sources_package_specification_multiple_cells(Language language)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
#!time
#i ""nuget:https://completelyFakerestoreSource""
");

            await kernel.SubmitCodeAsync(
                @"
#!time
#i ""nuget:https://completelyFakerestoreSource""
");

            events.Should().NotContainErrors();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Pound_i_nuget_allows_multiple_sources_package_specification_single_cell(Language language)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
#!time
#i ""nuget:https://completelyFakerestoreSource""
");

            events.Should().NotContainErrors();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Pound_i_nuget_allows_multiple_package_sources_to_be_specified_in_multiple_cells(Language language)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
#!time
#i ""nuget:https://completelyFakerestoreSource""
");
            events.Should().NotContainErrors();

            await kernel.SubmitCodeAsync(
                @"
#!time
#i ""nuget:https://anotherCompletelyFakerestoreSource""
");

            events.Should().NotContainErrors();
        }

        [Theory]
        [InlineData(Language.CSharp, "using Microsoft.ML.AutoML;")]
        [InlineData(Language.FSharp, "open Microsoft.ML.AutoML")]
        public async Task Pound_r_nuget_allows_duplicate_package_specifications_single_cell(Language language, string code)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"
#!time
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview""
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview""
");

            await kernel.SubmitCodeAsync(code);

            events.Should().NotContainErrors();
        }

        [Theory]
        [InlineData(Language.CSharp, "using Microsoft.ML.AutoML;")]
        [InlineData(Language.FSharp, "open Microsoft.ML.AutoML")]
        public async Task Pound_r_nuget_allows_duplicate_package_specifications_multiple_cells(Language language, string code)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview"""
            );
            events.Should().NotContainErrors();

            await kernel.SubmitCodeAsync(
                @"
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview""
");

            await kernel.SubmitCodeAsync(code);

            events.Should().NotContainErrors();
        }

        [Theory]
        [InlineData(Language.CSharp, "using Microsoft.ML.AutoML;")]
        [InlineData(Language.FSharp, "open Microsoft.ML.AutoML")]
        public async Task Pound_r_nuget_disallows_package_specifications_with_different_versions_single_cell(Language language, string code)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"
#!time
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview""
#r ""nuget:Microsoft.ML.AutoML,0.16.1-preview""
");

            await kernel.SubmitCodeAsync(code);

            events
                .OfType<ErrorProduced>()
                .Last()
                .Value
                .Should()
                .Be("Microsoft.ML.AutoML version 0.16.1-preview cannot be added because version 0.16.0-preview was added previously.");
        }

        [Theory]
        [InlineData(Language.CSharp, "using Microsoft.ML.AutoML;")]
        [InlineData(Language.FSharp, "open Microsoft.ML.AutoML")]
        public async Task Pound_r_nuget_disallows_package_specifications_with_different_versions_multiple_cells(Language language, string code)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(
                @"
#!time
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview""
");
            events.Should().NotContainErrors();

            await kernel.SubmitCodeAsync(
                @"
#!time
#r ""nuget:Microsoft.ML.AutoML,0.16.1-preview""
");

            await kernel.SubmitCodeAsync(code);

            events
                .OfType<ErrorProduced>()
                .Last()
                .Value
                .Should()
                .Be("Microsoft.ML.AutoML version 0.16.1-preview cannot be added because version 0.16.0-preview was added previously.");
        }

        [Theory]
        [InlineData(Language.CSharp, Language.FSharp)]
        [InlineData(Language.FSharp, Language.CSharp)]
        public async Task cell_with_nuget_and_code_continues_executions_on_right_kernel(Language first, Language second)
        {
            var csk =
                    new CSharpKernel()
                        .UseDefaultFormatting()
                        .UseNugetDirective()
                        .UseKernelHelpers()
                        .UseWho()
                        .LogEventsToPocketLogger();
            var fsk =
                    new FSharpKernel()
                        .UseDefaultFormatting()
                        .UseKernelHelpers()
                        .UseWho()
                        .UseDefaultNamespaces()
                        .LogEventsToPocketLogger();

            using var kernel =
                new CompositeKernel
                    {
                        first switch { Language.CSharp => csk, Language.FSharp => fsk },
                        second switch { Language.CSharp => csk, Language.FSharp => fsk }
                    }
                    .UseDefaultMagicCommands();

            kernel.DefaultKernelName = "csharp";

            var events = kernel.KernelEvents.ToSubscribedList();

            var command = new SubmitCode(@"#r ""nuget:Octokit, 0.32.0""
#r ""nuget:NodaTime, 2.4.6""
using Octokit;
using NodaTime;
using NodaTime.Extensions;
using XPlot.Plotly;");

            await kernel.SendAsync(command, CancellationToken.None);

            events.Should().NotContainErrors();

            events
                .Should()
                .ContainSingle<CommandSucceeded>(ch => ch.Command == command);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Pound_r_nuget_disallows_changing_version_of_loaded_dependent_packages(Language language)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"
#!time
#r ""nuget: Microsoft.ML, 1.4.0""
#r ""nuget:Microsoft.ML.AutoML,0.16.0""
#r ""nuget:Microsoft.Data.Analysis,0.1.0""
");
            events.Should().NotContainErrors();

            await kernel.SubmitCodeAsync(@"
#!time
#r ""nuget: Google.Protobuf, 3.10.1""
");

            events
                .OfType<ErrorProduced>()
                .Last()
                .Value
                .Should()
                .Be("Google.Protobuf version 3.10.1 cannot be added because version 3.10.0 was added previously.");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Pound_r_nuget_allows_using_version_of_loaded_dependent_packages(Language language)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"
#!time
#r ""nuget: Microsoft.ML, 1.4.0""
#r ""nuget:Microsoft.ML.AutoML,0.16.0""
#r ""nuget:Microsoft.Data.Analysis,0.1.0""
");
            events.Should().NotContainErrors();

            await kernel.SubmitCodeAsync(@"
#!time
#r ""nuget: Google.Protobuf, 3.10.0""
");
            events.Should().NotContainErrors();
        }

        [Theory]
        [InlineData(Language.CSharp, "using System.Text.Json;")]
        [InlineData(Language.FSharp, "open System.Text.Json")]
        public async Task Pound_r_nuget_with_System_Text_Json_should_succeed(Language language, string code)
        {
            var kernel = CreateKernel(language);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"
#r ""nuget:System.Text.Json, 4.6.0""
");

            await kernel.SubmitCodeAsync(code);
            // It should work, no errors, System.Text.Json is part of the shared framework
            events.Should().NotContainErrors();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Pound_r_nuget_with_no_version_should_not_get_the_oldest_package_version(Language language)
        {
            // #r "nuget: with no version specified should get the newest version of the package not the oldest:
            // For test purposes we evaluate the retrieved package is not the oldest version, since the newest may change over time.
            var kernel = CreateKernel(language);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"
#r ""nuget:Microsoft.DotNet.PlatformAbstractions""
");
            // It should work, no errors and the latest requested package should be added
            events.Should()
                  .NotContainErrors();

            events.OfType<PackageAdded>()
                  .Should()
                  .ContainSingle(e => e.PackageReference.PackageName == "Microsoft.DotNet.PlatformAbstractions" &&
                                      e.PackageReference.PackageVersion != "1.0.3");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Pound_r_nuget_with_no_version_displays_the_version_that_was_installed(Language language)
        {
            var kernel = CreateKernel(language);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"#r ""nuget:Its.Log""");

            events.Should().NotContainErrors();

            events.Should()
                  .ContainSingle<DisplayedValueUpdated>(e => e.Value.Equals("Installed package Its.Log version 2.10.1"));
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Pound_r_nuget_does_not_repeat_notifications_for_previous_r_nuget_submissions(Language language)
        {
            var kernel = CreateKernel(language);

            await kernel.SubmitCodeAsync(@"#r ""nuget:Its.Log""");

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(@"#r ""nuget:System.Text.Json""");

            events.Should().NotContainErrors();

            events.Should()
                  .NotContain(e =>
                                  e is DisplayedValueUpdated &&
                                  e.As<DisplayedValueUpdated>()
                                   .Value
                                   .As<string>()
                                   .StartsWith("Installing package Its.Log"));
        }

        [Theory]
        [InlineData(Language.CSharp)]
        //[InlineData(Language.FSharp)]   /// Reenable when --- https://github.com/dotnet/fsharp/issues/8775
        public async Task Pound_r_nuget_does_not_accept_invalid_keys(Language language)
        {
            var kernel = CreateKernel(language);

            // C# and F# should both fail, but the messages will be different because they handle it differently internally.
            var expectedMessage = language switch
            {
                Language.CSharp => "Metadata file 'nugt:System.Text.Json' could not be found",
                Language.FSharp => "interactive error Package manager key 'nugt' was not registered"
            };
            using var events = kernel.KernelEvents.ToSubscribedList();

            // nugt is an invalid provider key should fail
            await kernel.SubmitCodeAsync(@"#r ""nugt:System.Text.Json""");

            events.Should()
                  .ContainSingle<CommandFailed>()
                  .Which
                  .Message
                  .Should()
                  .Contain(expectedMessage);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task When_restore_fails_then_an_error_is_displayed(Language language)
        {
            var kernel = CreateKernel(language);

            var nonexistentPackageName = Guid.NewGuid().ToString("N");

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync($"#r nuget:{nonexistentPackageName},1.0.0");

            events.Should()
                  .ContainSingle<CommandFailed>()
                  .Which
                  .Message
                  .Should()
                  .Contain($"error NU1101: Unable to find package {nonexistentPackageName}. No packages exist with this id in source(s): ");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_can_load_assembly_referenced_from_refs_folder_in_nugetpackage(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => @"
#r ""nuget:Microsoft.ML.OnnxTransformer,1.4.0""

open System
open System.Numerics.Tensors
let inputValues = [| 12.0; 10.0; 17.0; 5.0 |]
let tInput = new DenseTensor<float>(inputValues.AsMemory(), new ReadOnlySpan<int>([|4|]))
tInput.Length
",
                Language.CSharp => @"
#r ""nuget:Microsoft.ML.OnnxTransformer,1.4.0""

using System;
using System.Numerics.Tensors;
var inputValues = new[] { 12f, 10f, 17f, 5f };
var tInput = new DenseTensor<float>(inputValues.AsMemory(), new ReadOnlySpan<int>(new[] { 4 }));
tInput.Length"
            };

            await SubmitCode(kernel, source);

            KernelEvents
                .Should()
                .ContainSingle<ReturnValueProduced>()
                .Which
                .Value
                .Should()
                .Be(4);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_can_load_platform_specific_assembly_in_nugetpackage(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => @"
#r ""nuget:System.Device.Gpio""
typeof<System.Device.Gpio.GpioController>.Assembly.Location
",
                Language.CSharp => @"
#r ""nuget:System.Device.Gpio""
typeof(System.Device.Gpio.GpioController).Assembly.Location
"
            };

            await SubmitCode(kernel, source);

            // Because this is platform specific there are platform specific results
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                KernelEvents
                    .Should()
                    .ContainSingle<ReturnValueProduced>()
                    .Which
                    .Value
                    .As<string>()
                    .EndsWith(@"runtimes\win\lib\netstandard2.0\System.Device.Gpio.dll")
                    .Should()
                    .Be(true);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                KernelEvents
                    .Should()
                    .ContainSingle<ReturnValueProduced>()
                    .Which
                    .Value
                    .As<string>()
                    .EndsWith(@"runtimes/linux/lib/netstandard2.0/System.Device.Gpio.dll")
                    .Should()
                    .Be(true);
            }
            // (OSPlatform.OSX is not supported by this library
        }
    }
}