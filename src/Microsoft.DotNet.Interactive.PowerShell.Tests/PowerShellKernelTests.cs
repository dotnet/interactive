// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;
using Language = Microsoft.DotNet.Interactive.Tests.Language;

namespace Microsoft.DotNet.Interactive.PowerShell.Tests;

public class PowerShellKernelTests : LanguageKernelTestBase
{
    private readonly string _allUsersCurrentHostProfilePath = Path.Combine(Path.GetDirectoryName(typeof(PSObject).Assembly.Location), "Microsoft.dotnet-interactive_profile.ps1");

    private readonly string _tableOutputOfCustomObjectTest =
        """
        <table>
          <thead>
            <tr>
              <th>
                <i>key</i>
              </th>
              <th>value</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>
                <div class="dni-plaintext">
                  <pre>prop1</pre>
                </div>
              </td>
              <td>
                <div class="dni-plaintext">
                  <pre>value1</pre>
                </div>
              </td>
            </tr>
            <tr>
              <td>
                <div class="dni-plaintext">
                  <pre>prop2</pre>
                </div>
              </td>
              <td>
                <div class="dni-plaintext">
                  <pre>value2</pre>
                </div>
              </td>
            </tr>
            <tr>
              <td>
                <div class="dni-plaintext">
                  <pre>prop3</pre>
                </div>
              </td>
              <td>
                <div class="dni-plaintext">
                  <pre>value3</pre>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
        """;

    public PowerShellKernelTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData(@"$x = New-Object -TypeName System.IO.FileInfo -ArgumentList c:\temp\some.txt", typeof(FileInfo))]
    [InlineData("$x = \"hello!\"", typeof(string))]
    public async Task TryGetVariable_unwraps_PowerShell_object(string code, Type expectedType)
    {
        using var kernel = new PowerShellKernel();

        await kernel.SubmitCodeAsync(code);

        kernel.TryGetValue("x", out object fi).Should().BeTrue();

        fi.Should().BeOfType(expectedType);
    }

    [Fact]
    public async Task PowerShell_progress_sends_updated_display_values()
    {
        var kernel = CreateKernel(Language.PowerShell);
        var command = new SubmitCode(@"
for ($j = 0; $j -le 4; $j += 4 ) {
    $p = $j * 25
    Write-Progress -Id 1 -Activity 'Search in Progress' -Status ""$p% Complete"" -PercentComplete $p
    Start-Sleep -Milliseconds 300
}
");
        var result = await kernel.SendAsync(command);

        Assert.Collection(result.Events,
                          e => e.Should().BeOfType<CodeSubmissionReceived>(),
                          e => e.Should().BeOfType<CompleteCodeSubmissionReceived>(),
                          e => e.Should().BeOfType<DisplayedValueProduced>().Which
                                .Value.Should().BeOfType<string>().Which
                                .Should().Match("* Search in Progress* 0% Complete* [ * ] *"),
                          e => e.Should().BeOfType<DisplayedValueUpdated>().Which
                                .Value.Should().BeOfType<string>().Which
                                .Should().Match("* Search in Progress* 100% Complete* [ooo*ooo] *"),
                          e => e.Should().BeOfType<DisplayedValueUpdated>().Which
                                .Value.Should().BeOfType<string>().Which
                                .Should().Be(string.Empty),
                          e => e.Should().BeOfType<CommandSucceeded>());
    }

    [Fact]
    public async Task When_command_is_not_recognized_then_the_command_fails()
    {
        using var kernel = CreateKernel(Language.PowerShell);

        var result = await kernel.SendAsync(new SubmitCode("oops"));

        result.Events.Last().Should().BeOfType<CommandFailed>();
    }

    [Fact]
    public async Task When_code_produces_errors_then_the_command_fails()
    {
        using var kernel = CreateKernel(Language.PowerShell);

        var result = await kernel.SendAsync(new SubmitCode("Get-ChildItem oops"));

        result.Events.Last().Should().BeOfType<CommandFailed>()
              .Which.Message
              .Should().Match("Cannot find path '*oops' because it does not exist.");
    }

    [Fact]
    public async Task Errors_from_previous_submissions_are_not_shown()
    {
        using var kernel = CreateKernel(Language.PowerShell);

        await kernel.SendAsync(new SubmitCode("Get-ChildItem oops1"));

        var result = await kernel.SendAsync(new SubmitCode("Get-ChildItem oops2"));

        result.Events.Last().Should().BeOfType<CommandFailed>()
              .Which.Message
              .Should().NotContain("oops1");
    }

    [Fact]
    public async Task PowerShell_token_variables_work()
    {
        var kernel = CreateKernel(Language.PowerShell);

        await kernel.SendAsync(new SubmitCode("echo /this/is/a/path"));
        await kernel.SendAsync(new SubmitCode("$$; $^"));

        KernelEvents.Should().SatisfyRespectively(
            e => e.Should()
                .BeOfType<CodeSubmissionReceived>()
                .Which.Code
                .Should().Be("echo /this/is/a/path"),
            e => e.Should()
                .BeOfType<CompleteCodeSubmissionReceived>()
                .Which.Code
                .Should().Be("echo /this/is/a/path"),
            e => e.Should()
                .BeOfType<StandardOutputValueProduced>()
                .Which
                .FormattedValues
                .Should()
                .ContainSingle(f => f.Value == "/this/is/a/path" + Environment.NewLine),
            e => e.Should().BeOfType<CommandSucceeded>(),
            e => e.Should()
                .BeOfType<CodeSubmissionReceived>()
                .Which.Code
                .Should().Be("$$; $^"),
            e => e.Should()
                .BeOfType<CompleteCodeSubmissionReceived>()
                .Which.Code
                .Should().Be("$$; $^"),
            e => e.Should()
                .BeOfType<StandardOutputValueProduced>()
                .Which
                .FormattedValues
                .Should()
                .ContainSingle(f => f.Value == "/this/is/a/path" + Environment.NewLine),
            e => e.Should()
                .BeOfType<StandardOutputValueProduced>()
                .Which
                .FormattedValues
                .Should()
                .ContainSingle(f => f.Value == "echo" + Environment.NewLine),
            e => e.Should().BeOfType<CommandSucceeded>());
    }

    [Fact]
    public async Task PowerShell_get_history_should_work()
    {
        var kernel = CreateKernel(Language.PowerShell);

        await kernel.SendAsync(new SubmitCode("Get-Verb > $null"));
        await kernel.SendAsync(new SubmitCode("echo bar > $null"));
        var result = await kernel.SendAsync(new SubmitCode("Get-History | % CommandLine"));

        var outputs = result.Events
            .OfType<StandardOutputValueProduced>();

        outputs.Should().SatisfyRespectively(
            e => e.FormattedValues
                .Should()
                .ContainSingle(f => f.Value == "Get-Verb > $null" + Environment.NewLine),
            e => e.FormattedValues
                .Should()
                .ContainSingle(f => f.Value == "echo bar > $null" + Environment.NewLine));
    }

    [Fact]
    public async Task PowerShell_native_executable_output_is_collected()
    {
        var kernel = CreateKernel(Language.PowerShell);

        var command = new SubmitCode("dotnet --help");

        await kernel.SendAsync(command);

        var outputs = KernelEvents.OfType<StandardOutputValueProduced>();

        outputs.Should().HaveCountGreaterThan(1);

        string.Join("",
                outputs
                    .SelectMany(e => e.FormattedValues.Select(v => v.Value))
            ).ToLowerInvariant()
            .Should()
            .ContainAll("build-server", "restore");
    }

    [Fact]
    public async Task GetCorrectProfilePaths()
    {
        using var kernel = new PowerShellKernel().UseProfiles();

        // Set variables we will retrieve later.
        await kernel.SubmitCodeAsync("$currentUserCurrentHost = $PROFILE.CurrentUserCurrentHost");
        await kernel.SubmitCodeAsync("$allUsersCurrentHost = $PROFILE.AllUsersCurrentHost");

        var valueProduced = await kernel.RequestValueAsync("currentUserCurrentHost");
        valueProduced.Value.Should().BeOfType<string>();
        string currentUserCurrentHost = valueProduced.Value.As<string>();

        // Get $PROFILE default.
        valueProduced = await kernel.RequestValueAsync("PROFILE");
        valueProduced.Value.Should().BeOfType<string>();
        string profileDefault = valueProduced.Value.As<string>();

        // Check that $PROFILE is not null or empty and it is the same as
        // $PROFILE.CurrentUserCurrentHost
        profileDefault.Should().NotBeNullOrEmpty();
        profileDefault.Should().Be(currentUserCurrentHost);

        valueProduced = await kernel.RequestValueAsync("allUsersCurrentHost");
        valueProduced.Value.Should().BeOfType<string>();
        string allUsersCurrentHost = valueProduced.Value.As<string>();

        // Check that $PROFILE.AllUsersCurrentHost is what we expect it is:
        // $PSHOME + Microsoft.dotnet-interactive_profile.ps1
        allUsersCurrentHost.Should().Be(_allUsersCurrentHostProfilePath);
    }

    [Fact]
    public async Task VerifyAllUsersProfileRuns()
    {
        var randomVariableName = Path.GetRandomFileName().Split('.')[0];
        File.WriteAllText(_allUsersCurrentHostProfilePath, $"$global:{randomVariableName} = $true");

        try
        {
            using var kernel = new PowerShellKernel().UseProfiles();

            // trigger first time setup.
            await kernel.SubmitCodeAsync("Get-Date");

            var valueProduced = await kernel.RequestValueAsync(randomVariableName);

            valueProduced.Value.Should().BeOfType<bool>();
            valueProduced.Value.As<bool>().Should().BeTrue();
        }
        finally
        {

            File.Delete(_allUsersCurrentHostProfilePath);
        }
    }

    [Fact]
    public async Task RequestValueInfos_only_returns_user_defined_values()
    {
        using var kernel = CreateKernel(Language.PowerShell);
        await kernel.SendAsync(new SubmitCode("$theAnswer = 42"));

        var result = await kernel.SendAsync(new RequestValueInfos());

        result.Events
              .Should()
              .ContainSingle<ValueInfosProduced>()
              .Which
              .ValueInfos
              .Should()
              .ContainSingle()
              .Which
              .Name
              .Should()
              .Be("theAnswer");
    }

    [Fact]
    public async Task Powershell_non_standard_mimetypes_work()
    {
      var kernel = CreateKernel(Language.PowerShell);

      var vegaLightRequest =
      """
        {
          "`$schema": "https://vega.github.io/schema/vega-lite/v5.json",
          "description": "Horizon Graph with 2 layers. (See https://idl.cs.washington.edu/papers/horizon/ for more details on Horizon Graphs.)",
          "width": 600,
          "height": 100,
          "data": {
            "values": [
              {"x": 1,  "y": 28}, {"x": 2,  "y": 55},
              {"x": 3,  "y": 43}, {"x": 4,  "y": 91},
              {"x": 5,  "y": 81}, {"x": 6,  "y": 53},
              {"x": 7,  "y": 19}, {"x": 8,  "y": 87},
              {"x": 9,  "y": 52}, {"x": 10, "y": 48},
              {"x": 11, "y": 24}, {"x": 12, "y": 49},
              {"x": 13, "y": 87}, {"x": 14, "y": 66},
              {"x": 15, "y": 17}, {"x": 16, "y": 27},
              {"x": 17, "y": 68}, {"x": 18, "y": 16},
              {"x": 19, "y": 49}, {"x": 20, "y": 15}
            ]
          },
          "encoding": {
            "x": {
              "field": "x", "type": "quantitative",
              "scale": {"zero": false, "nice": false}
            },
            "y": {
              "field": "y", "type": "quantitative",
              "scale": {"domain": [0,50]},
              "axis": {"title": "y"}
            }
          },
          "layer": [{
            "mark": {"type": "area", "clip": true, "orient": "vertical", "opacity": 0.6}
          }, {
            "transform": [{"calculate": "datum.y - 50", "as": "ny"}],
            "mark": {"type": "area", "clip": true, "orient": "vertical"},
            "encoding": {
              "y": {
                "field": "ny", "type": "quantitative",
                "scale": {"domain": [0,50]}
              },
              "opacity": {"value": 0.3}
            }
          }],
          "config": {
            "area": {"interpolate": "monotone"}
          }
        }
      """;
      var result = await kernel.SendAsync(new SubmitCode($"\'{vegaLightRequest}\' | Out-Display -MimeType 'application/vnd.vegalite.v5+json'"));

      result.Events.Should()
            .ContainSingle<DisplayedValueProduced>()
            .Which
            .FormattedValues
            .Should()
            .ContainSingle(v => v.MimeType == "application/vnd.vegalite.v5+json")
            .Which
            .Value.RemoveStyleElement()
            .Should()
            .Be(vegaLightRequest);
    }

    [Theory]
    [InlineData(" -MimeType 'text/html'")]
    [InlineData("")]
    public async Task Powershell_CustomObject_Is_Formatted_Correctly(string mimeTypeParameter)
    {
        // Arrange
        var kernel = CreateKernel(Language.PowerShell);
        var code = "[pscustomobject]@{ prop1 = 'value1'; prop2 = 'value2'; prop3 = 'value3' } | Out-Display" + mimeTypeParameter;

        // Act
        var result = await kernel.SendAsync(new SubmitCode(code));

        // Assert
        result.Events.Should()
              .ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.MimeType == HtmlFormatter.MimeType)
              .Which
              .Value.RemoveStyleElement()
              .Should()
              .BeEquivalentHtmlTo(_tableOutputOfCustomObjectTest);
    }

    [Fact]
    public async Task PowerShell_objects_without_custom_formatters_use_native_formatting()
    {
        // Arrange
        var kernel = CreateKernel(Language.PowerShell);
        
        // Act - Get-Process returns objects without custom formatters and should use PowerShell native formatting
        var result = await kernel.SendAsync(new SubmitCode("Get-Process | Select-Object -First 1"));

        // Assert - Should produce StandardOutputValueProduced (native PowerShell formatting) not DisplayedValueProduced
        var outputs = result.Events.OfType<StandardOutputValueProduced>().ToList();
        outputs.Should().NotBeEmpty("Get-Process output should use native PowerShell formatting");
        
        // The output should contain typical PowerShell table formatting headers
        var allOutput = string.Join("", outputs.SelectMany(e => e.FormattedValues.Select(v => v.Value)));
        allOutput.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PowerShell_objects_with_custom_formatters_use_custom_formatting()
    {
        // Arrange
        var kernel = CreateKernel(Language.PowerShell);
        
        // Register a custom formatter for FileInfo
        Formatter.Register<FileInfo>((fileInfo, writer) =>
        {
            writer.Write($"CUSTOM: {fileInfo.Name}");
        }, PlainTextFormatter.MimeType);

        try
        {
            // Act - Create a FileInfo object which now has a custom formatter
            var result = await kernel.SendAsync(new SubmitCode("[System.IO.FileInfo]::new('test.txt')"));

            // Assert - Should produce DisplayedValueProduced (custom formatter used)
            var displayedValues = result.Events.OfType<DisplayedValueProduced>().ToList();
            displayedValues.Should().ContainSingle("FileInfo with custom formatter should use Display");
            
            var output = displayedValues.First().FormattedValues.First(f => f.MimeType == PlainTextFormatter.MimeType).Value;
            output.Should().Contain("CUSTOM: test.txt");
        }
        finally
        {
            // Clean up - reset formatters
            Formatter.ResetToDefault();
        }
    }

    [Fact]
    public async Task PowerShell_Format_Table_works_with_native_formatting()
    {
        // Arrange
        var kernel = CreateKernel(Language.PowerShell);
        
        // Act - Use Format-Table explicitly
        var result = await kernel.SendAsync(new SubmitCode(
            "[pscustomobject]@{ Name='Item1'; Value=10 },[pscustomobject]@{ Name='Item2'; Value=20 } | Format-Table"));

        // Assert - Should produce StandardOutputValueProduced with table formatting
        var outputs = result.Events.OfType<StandardOutputValueProduced>().ToList();
        outputs.Should().NotBeEmpty("Format-Table output should use native PowerShell formatting");
        
        var allOutput = string.Join("", outputs.SelectMany(e => e.FormattedValues.Select(v => v.Value)));
        allOutput.Should().Contain("Name");
        allOutput.Should().Contain("Value");
    }
}