// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests;
#pragma warning disable 8509
public class LanguageKernelPackageTests : LanguageKernelTestBase
{
    public LanguageKernelPackageTests(ITestOutputHelper output) : base(output)
    {
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

        var command = new SubmitCode("#r \"nuget:SixLabors.ImageSharp, 1.0.2\"" + Environment.NewLine + assignment);
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

        var code = $"""
                    #r "nuget:Microsoft.Extensions.Logging, 2.2.0"
                    {expression}
                    """;
        var command = new SubmitCode(code);

        var result = await kernel.SendAsync(command);

        result.Events
              .Should()
              .ContainSingle<PackageAdded>(
                  e =>
                      e.PackageReference.PackageName == "Microsoft.Extensions.Logging" &&
                      e.PackageReference.PackageVersion == "2.2.0");
    }

    [Fact]
    public async Task Loads_native_dependencies_from_nugets()
    {
        using var kernel = CreateKernel();

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
            .ContainSingle<StandardOutputValueProduced>(
                e => e.FormattedValues.Any(v => v.Value.Contains("success")));
    }

    [Fact]
    public async Task Dependency_version_conflicts_are_resolved_correctly()
    {
        var kernel = CreateKernel(Language.CSharp);

        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync(@"#!time
#r ""nuget:Microsoft.ML.AutoML,0.16.0-preview""
#r ""nuget:Microsoft.Data.Analysis,0.4.0""
");

        await kernel.SubmitCodeAsync($@"
using Microsoft.Data.Analysis;
using static {typeof(PocketViewTags).FullName};
using {typeof(PocketView).Namespace};");

        await kernel.SubmitCodeAsync(@"
using Microsoft.AspNetCore.Html;
Formatter.Register<DataFrame>((df, writer) =>
{
    var headers = new List<IHtmlContent>();
    headers.Add(th(i(""index"")));
    headers.AddRange(df.Columns.Select(c => (IHtmlContent) th(c)));
    var rows = new List<List<IHtmlContent>>();
    var take = 20;
    for (var i = 0; i < Math.Min(take, df.Rows.Count); i++)
    {
        var cells = new List<IHtmlContent>();
        cells.Add(td(i));
        foreach (var obj in df.Rows[i])
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

        await kernel.SubmitCodeAsync("""
                                     #r "nuget:"
                                     """);

        events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Be("(1,4): error DNI210: Unable to parse package reference: \"nuget:\"");
    }

    [Fact]
    public async Task Pound_r_is_not_treated_as_pound_r_nuget_by_csharp_kernel_if_assembly_path_happens_to_contain_the_string_nuget()
    {
        var kernel = CreateCSharpKernel();

        var result =
            await kernel.SubmitCodeAsync(
                """
                #r "C:\Users\abcde\.nuget\packages\package\1.0.0\package.dll"
                """);

        result.Events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .ContainAll("Metadata file", "could not be found")
            .And
            .NotContain("DNI");
    }

    [Fact]
    public async Task Pound_r_is_not_treated_as_pound_r_nuget_by_fsharp_kernel_if_assembly_path_happens_to_contain_the_string_nuget()
    {
        var kernel = CreateKernel(Language.FSharp);

        var result =
            await kernel.SubmitCodeAsync(
                """
                #r "C:\Users\abcde\.nuget\packages\package\1.0.0\package.dll"
                """);

        result.Events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Contain("is not a valid assembly name")
            .And
            .NotContain("DNI");
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
            .Be("(3,4): error DNI210: Unable to parse package reference: \"nuget:,1.0.0\"");
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

        await kernel.SubmitCodeAsync(@"
#i ""nuget:https://completelyFakerestoreSource1""
#i ""nuget:https://completelyFakerestoreSource2""
");
        events.OfType<DisplayEvent>().Select(e => e.GetType()).Should().ContainInOrder(typeof(DisplayedValueProduced), typeof(DisplayedValueUpdated));
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task Pound_i_nuget_with_multi_submissions_combines_the_text_produced(Language language)
    {
        var kernel = CreateKernel(language);

        await kernel.SubmitCodeAsync(@"
#i ""nuget:https://completelyFakerestoreSourceCommand1.1""
#i ""nuget:https://completelyFakerestoreSourceCommand1.2""
");

        var result = await kernel.SubmitCodeAsync(@"
#i ""nuget:https://completelyFakerestoreSourceCommand2.1""
");

        var expectedList = new[]
        {
            "https://completelyFakerestoreSourceCommand1.1",
            "https://completelyFakerestoreSourceCommand1.2",
            "https://completelyFakerestoreSourceCommand2.1"
        };

        result.Events.OfType<DisplayedValueProduced>()
              .Should()
              .ContainSingle(v => v.Value is InstallPackagesMessage)
              .Which.Value
              .As<InstallPackagesMessage>()
              .RestoreSources
              .Aggregate((s, acc) => acc + " & " + s)
              .Should()
              .ContainAll(expectedList);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task Pound_i_nuget_with_multi_submissions_combines_the_Text_Updates(Language language)
    {
        var kernel = CreateKernel(language);

        await kernel.SubmitCodeAsync(@"
#i ""nuget:https://completelyFakerestoreSourceCommand1.1""
#i ""nuget:https://completelyFakerestoreSourceCommand1.2""
");

        var result = await kernel.SubmitCodeAsync(@"
#i ""nuget:https://completelyFakerestoreSourceCommand2.1""
#i ""nuget:https://completelyFakerestoreSourceCommand2.2""
");

        var expectedList = new[]
        {
            "https://completelyFakerestoreSourceCommand1.1",
            "https://completelyFakerestoreSourceCommand1.2",
            "https://completelyFakerestoreSourceCommand2.1",
            "https://completelyFakerestoreSourceCommand2.2"
        };

        result.Events.OfType<DisplayedValueProduced>()
              .Should()
              .ContainSingle(v => v.Value is InstallPackagesMessage)
              .Which.Value
              .As<InstallPackagesMessage>()
              .RestoreSources
              .Should()
              .BeEquivalentTo(expectedList);
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
    //[InlineData(Language.CSharp, "using SixLabors.ImageSharp;")]
    [InlineData(Language.FSharp, "open SixLabors.ImageSharp")]
    public async Task Pound_r_nuget_allows_duplicate_package_specifications_single_cell(Language language, string code)
    {
        var kernel = CreateKernel(language);

        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync(@"
#!time
#r ""nuget:SixLabors.ImageSharp,1.0.2""
#r ""nuget:SixLabors.ImageSharp,1.0.2""
");

        await kernel.SubmitCodeAsync(code);

        events.Should().NotContainErrors();
    }

    [Theory]
    [InlineData(Language.CSharp, "using SixLabors.ImageSharp;")]
    [InlineData(Language.FSharp, "open SixLabors.ImageSharp")]
    public async Task Pound_r_nuget_allows_duplicate_package_specifications_multiple_cells(Language language, string code)
    {
        var kernel = CreateKernel(language);

        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync(
            @"
#r ""nuget:SixLabors.ImageSharp,1.0.2"""
        );
        events.Should().NotContainErrors();

        await kernel.SubmitCodeAsync(
            @"
#r ""nuget:SixLabors.ImageSharp,1.0.2""
");

        await kernel.SubmitCodeAsync(code);

        events.Should().NotContainErrors();
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task Pound_r_nuget_disallows_package_specifications_with_different_versions_single_cell(Language language)
    {
        var kernel = CreateKernel(language);

        var results = await kernel.SubmitCodeAsync(@"
#!time
#r ""nuget:SixLabors.ImageSharp, 1.0.1""
#r ""nuget:SixLabors.ImageSharp, 1.0.2""
");

        results.Events.Should()
               .ContainSingle<CommandFailed>()
               .Which
               .Message
               .Should()
               .Be("SixLabors.ImageSharp version 1.0.2 cannot be added because version 1.0.1 was added previously.");

    }

    [Theory]
    [InlineData(Language.CSharp, "using SixLabors.ImageSharp;", "*The type or namespace name 'SixLabors' could not be found*")]
    [InlineData(Language.FSharp, "open SixLabors.ImageSharp", "*The namespace or module 'SixLabors' is not defined.")]
    public async Task Pound_r_nuget_with_different_versions_in_a_single_cell_fails_package_restore(Language language, string code, string errorMessage)
    {
        var kernel = CreateKernel(language);

        await kernel.SubmitCodeAsync(@"
#!time
#r ""nuget:SixLabors.ImageSharp, 1.0.1""
#r ""nuget:SixLabors.ImageSharp, 1.0.2""
");

        var results = await kernel.SubmitCodeAsync(code);

        results.Events.Should()
               .ContainSingle<CommandFailed>()
               .Which
               .Message
               .Should()
               .Match(errorMessage);
    }

    [Theory]
    [InlineData(Language.CSharp, "using SixLabors.ImageSharp;")]
    [InlineData(Language.FSharp, "open SixLabors.ImageSharp")]
    public async Task Pound_r_nuget_disallows_package_specifications_with_different_versions_multiple_cells(Language language, string code)
    {
        var kernel = CreateKernel(language);

        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync(
            @"
#!time
#r ""nuget:SixLabors.ImageSharp, 1.0.1""
");
        events.Should().NotContainErrors();

        await kernel.SubmitCodeAsync(
            @"
#!time
#r ""nuget:SixLabors.ImageSharp, 1.0.2""
");

        await kernel.SubmitCodeAsync(code);

        events.Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Be("SixLabors.ImageSharp version 1.0.2 cannot be added because version 1.0.1 was added previously.");
    }

    [Theory]
    [InlineData(Language.CSharp, Language.FSharp)]
    [InlineData(Language.FSharp, Language.CSharp)]
    public async Task cell_with_nuget_and_code_continues_executions_on_right_kernel(Language first, Language second)
    {
        var csk =
            new CSharpKernel()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseWho()
                .LogEventsToPocketLogger();
        var fsk =
            new FSharpKernel()
                .UseDefaultFormatting()
                .UseKernelHelpers()
                .UseWho()
                .LogEventsToPocketLogger();

        using var kernel =
            new CompositeKernel
                {
                    first switch { Language.CSharp => csk, Language.FSharp => fsk },
                    second switch { Language.CSharp => csk, Language.FSharp => fsk }
                }
                .UseDefaultMagicCommands();

        kernel.DefaultKernelName = "csharp";

        var command = new SubmitCode(@"#r ""nuget:Octokit, 0.50.0""
#r ""nuget:NodaTime, 3.1.0""

using Octokit;
using NodaTime;
using NodaTime.Extensions;");

        var result = await kernel.SendAsync(command, CancellationToken.None);

        result.Events.Should().NotContainErrors();

        result.Events
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
#r ""nuget:Microsoft.ML, 1.7.1""
#r ""nuget:Microsoft.ML.AutoML, 0.19.1""
#r ""nuget:Microsoft.Data.Analysis, 0.19.1""
");
        events.Should().NotContainErrors();

        await kernel.SubmitCodeAsync(@"
#!time
#r ""nuget: Google.Protobuf, 3.10.1""
");

        events.Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Be("Google.Protobuf version 3.10.1 cannot be added because version 3.19.4 was added previously.");
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
#r ""nuget:Microsoft.ML, 1.7.1""
#r ""nuget:Microsoft.ML.AutoML, 0.19.1""
#r ""nuget:Microsoft.Data.Analysis, 0.19.1""
");
        events.Should().NotContainErrors();

        await kernel.SubmitCodeAsync(@"
#!time
#r ""nuget: Google.Protobuf, 3.19.4""
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
#r ""nuget:System.Text.Json, 6.0.0""
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
#r ""nuget:System.Text.Json""
");
        // It should work, no errors and the latest requested package should be added
        events.Should()
            .NotContainErrors();

        events.OfType<PackageAdded>()
            .Should()
            .ContainSingle(e => e.PackageReference.PackageName == "System.Text.Json" &&
                                e.PackageReference.PackageVersion != "5.0.2");
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task Pound_r_nuget_with_no_version_displays_the_version_that_was_installed(Language language)
    {
        var expectedList = new[]
        {
            "Its.Log, 2.10.1"
        };

        var kernel = CreateKernel(language);
        using var events = kernel.KernelEvents.ToSubscribedList();
        await kernel.SubmitCodeAsync(@"#r ""nuget:Its.Log""");

        events.Should().NotContainErrors();

        events.OfType<DisplayedValueUpdated>()
            .Where(v => v.Value is InstallPackagesMessage)
            .Last().Value
            .As<InstallPackagesMessage>()
            .InstalledPackages
            .Aggregate((p, acc) => acc + " & " + p)
            .Should()
            .ContainAll(expectedList);

        events.OfType<PackageAdded>()
            .Should()
            .ContainSingle(e => e.PackageReference.PackageName == "Its.Log" &&
                                e.PackageReference.PackageVersion == "2.10.1");
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
                    .Value.ToString()
                    .StartsWith("Installing package Its.Log"));
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task Pound_r_nuget_does_not_accept_invalid_keys(Language language)
    {
        var kernel = CreateKernel(language);

        using var events = kernel.KernelEvents.ToSubscribedList();

        // nugt is an invalid provider key should fail
        await kernel.SubmitCodeAsync(@"#r ""nugt:System.Text.Json""");

        events.Should()
            .ContainSingle<DiagnosticsProduced>(d => d.Diagnostics.Count > 0)
            .Which
            .FormattedDiagnostics
            .Should()
            .ContainSingle(fv => true)
            .Which
            .Value
            .Should()
            .ContainAll(
                // C# and F# should both fail, but the messages will be different because they handle it differently internally.
                language switch
                {
                    Language.CSharp => new[] { "CS0006", "nugt:System.Text.Json" },
                    Language.FSharp => new[] { "interactive error", "nugt" }
                }
            );
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
            .Contain("error NU1101:", nonexistentPackageName);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task it_can_load_assembly_referenced_from_refs_folder_in_nuget_package(Language language)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.FSharp => """
#r "nuget:Microsoft.ML.OnnxTransformer,1.4.0"

open System
open System.Numerics.Tensors
let inputValues = [| 12.0; 10.0; 17.0; 5.0 |]
let tInput = new DenseTensor<float>(inputValues.AsMemory(), new ReadOnlySpan<int>([|4|]))
tInput.Length
""",
            Language.CSharp => """
#r "nuget:Microsoft.ML.OnnxTransformer,1.4.0"

using System;
using System.Numerics.Tensors;
var inputValues = new[] { 12f, 10f, 17f, 5f };
var tInput = new DenseTensor<float>(inputValues.AsMemory(), new ReadOnlySpan<int>(new[] { 4 }));
tInput.Length
"""
        };

        var result = await SubmitCode(kernel, source);

        result.Events
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
    public async Task it_can_load_platform_specific_assembly_in_nuget_package(Language language)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.FSharp => """
#r "nuget:System.Device.Gpio,1.0.0"
typeof<System.Device.Gpio.GpioController>.Assembly.Location
""",
            Language.CSharp => """
#r "nuget:System.Device.Gpio,1.0.0"
typeof(System.Device.Gpio.GpioController).Assembly.Location
"""
        };

        var result = await SubmitCode(kernel, source);

        // Because this is platform specific there are platform specific results
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            result.Events
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
            result.Events
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

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    [InlineData(Language.PowerShell)]
    public async Task Pound_r_nuget_works_immediately_after_a_language_selector(Language defaultLanguageKernel)
    {
        var kernel = CreateCompositeKernel(defaultLanguageKernel);

        var code = @"
#!csharp
#r ""nuget:Newtonsoft.Json,11.0.2""
";

        var command = new SubmitCode(code);

        using var events = kernel.KernelEvents.ToSubscribedList();

        var result = await kernel.SendAsync(command);

        events.Should().NotContainErrors();

        using var _ = new AssertionScope();

        events
            .Should()
            .ContainSingle<PackageAdded>()
            .Which
            .PackageReference
            .PackageName
            .Should()
            .Be("Newtonsoft.Json");
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task Pound_r_nuget_should_display_only_requested_packages_for_first_submission(Language defaultLanguageKernel)
    {
        var kernel = CreateCompositeKernel(defaultLanguageKernel);

        var code = @"
#r ""nuget:Microsoft.ML.OnnxTransformer,1.4.0""
";

        var command = new SubmitCode(code);

        using var events = kernel.KernelEvents.ToSubscribedList();
        var result = await kernel.SendAsync(command);

        using var _ = new AssertionScope();

        var expectedDisplayed = new[]
        {
            "Microsoft.ML.OnnxTransformer, 1.4.0"
        };

        events.OfType<DisplayedValueUpdated>()
            .Where(v => v.Value is InstallPackagesMessage)
            .Last().Value
            .As<InstallPackagesMessage>()
            .InstalledPackages
            .Should()
            .BeEquivalentTo(expectedDisplayed);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task Pound_r_nuget_should_display_only_requested_packages_for_subsequent_submission(Language defaultLanguageKernel)
    {
        var kernel = CreateCompositeKernel(defaultLanguageKernel);

        var codeFirstSubmission = @"
#r ""nuget:Microsoft.ML.OnnxTransformer,1.4.0""
";
        var command1 = new SubmitCode(codeFirstSubmission);
        var _result1 = await kernel.SendAsync(command1);

        var codeSecondSubmission = @"
#r ""nuget:Google.Protobuf, 3.5.1""
";
        var command2 = new SubmitCode(codeSecondSubmission);

        using var events = kernel.KernelEvents.ToSubscribedList();
        var result2 = await kernel.SendAsync(command2);

        using var _ = new AssertionScope();

        var expected = new[]
        {
            "google.protobuf, 3.5.1"
        };

        events.OfType<DisplayedValueUpdated>()
            .Where(v => v.Value is InstallPackagesMessage)
            .Last().Value
            .As<InstallPackagesMessage>()
            .InstalledPackages
            .Should()
            .BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task Pound_r_nuget_should_display_only_requested_packages_for_third_submission(Language defaultLanguageKernel)
    {
        var kernel = CreateCompositeKernel(defaultLanguageKernel);

        var codeFirstSubmission = @"
#r ""nuget:Microsoft.ML.OnnxTransformer,1.4.0""
";
        var command1 = new SubmitCode(codeFirstSubmission);
        var _result1 = await kernel.SendAsync(command1);

        var codeSecondSubmission = @"
#r ""nuget:Google.Protobuf, 3.5.1""
";
        var command2 = new SubmitCode(codeSecondSubmission);
        var result2 = await kernel.SendAsync(command2);

        var codeThirdSubmission = @"
#r ""nuget:Google.Protobuf, 3.5.1""
#r ""nuget:Microsoft.ML.OnnxTransformer, 1.4.0""
";
        var command3 = new SubmitCode(codeThirdSubmission);

        using var events = kernel.KernelEvents.ToSubscribedList();
        var result3 = await kernel.SendAsync(command3);

        using var _ = new AssertionScope();

        var expectedDisplayed = new[]
        {
            "google.protobuf, 3.5.1",
            "Microsoft.ML.OnnxTransformer, 1.4.0"
        };

        events.OfType<DisplayedValueUpdated>()
            .Where(v => v.Value is InstallPackagesMessage)
            .Last().Value
            .As<InstallPackagesMessage>()
            .InstalledPackages
            .Should()
            .BeEquivalentTo(expectedDisplayed);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task Pound_r_nuget_should_error_when_trying_to_specify_a_different_version_of_an_already_loaded_package(Language defaultLanguageKernel)
    {
        var kernel = CreateCompositeKernel(defaultLanguageKernel);

        var codeFirstSubmission = @"
#r ""nuget:Microsoft.ML.OnnxTransformer,1.4.0""
";
        var command1 = new SubmitCode(codeFirstSubmission);
        var _result1 = await kernel.SendAsync(command1);

        var codeSecondSubmission = @"
#r ""nuget:Google.Protobuf, 3.5.0""
";
        var command2 = new SubmitCode(codeSecondSubmission);

        using var events = kernel.KernelEvents.ToSubscribedList();
        var result2 = await kernel.SendAsync(command2);

        using var _ = new AssertionScope();

        events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Be("Google.Protobuf version 3.5.0 cannot be added because version 3.5.1 was added previously.");
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task Pound_r_nuget_should_error_when_trying_to_specify_a_different_version_of_an_already_specified_package(Language defaultLanguageKernel)
    {
        var kernel = CreateCompositeKernel(defaultLanguageKernel);

        var codeFirstSubmission = @"
#r ""nuget:Google.Protobuf, 3.5.0""
#r ""nuget:Google.Protobuf, 3.5.1""
";
        var command = new SubmitCode(codeFirstSubmission);

        using var events = kernel.KernelEvents.ToSubscribedList();
        var result = await kernel.SendAsync(command);

        using var _ = new AssertionScope();

        events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Be("Google.Protobuf version 3.5.1 cannot be added because version 3.5.0 was added previously.");
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task Pound_r_nuget_should_not_error_when_trying_to_load_again_same_package_with_wildcard(Language defaultLanguageKernel)
    {
        var kernel = CreateCompositeKernel(defaultLanguageKernel);

        var codeFirstSubmission = @"
#r ""nuget:Google.Protobuf, *-*""
";


        await kernel.SendAsync(new SubmitCode(codeFirstSubmission));


        var codeSecondSubmission = @"
#r ""nuget:Google.Protobuf, *-*""
";

        var result = await kernel.SendAsync(new SubmitCode(codeSecondSubmission));

        using var _ = new AssertionScope();

        result.Events
              .Should()
              .NotContainErrors();
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task Pound_r_nuget_should__trying_to_load_again_same_package_with_wildcard_after_loading_a_specific_version_reuses_the_previously_resolved(Language defaultLanguageKernel)
    {
        var kernel = CreateCompositeKernel(defaultLanguageKernel);

        var codeFirstSubmission = @"
#r ""nuget:Google.Protobuf, 3.5.1""
";

        await kernel.SendAsync(new SubmitCode(codeFirstSubmission));

        var codeSecondSubmission = @"
#r ""nuget:Google.Protobuf, *-*""
";

        var result = await kernel.SendAsync(new SubmitCode(codeSecondSubmission));

        using var _ = new AssertionScope();

        var expectedDisplayed = new[]
        {
            "Google.Protobuf, 3.5.1"
        };

        result.Events
              .OfType<DisplayedValueUpdated>()
              .Last(v => v.Value is InstallPackagesMessage)
              .Value
              .As<InstallPackagesMessage>()
              .InstalledPackages
              .Should()
              .BeEquivalentTo(expectedDisplayed);
    }
}