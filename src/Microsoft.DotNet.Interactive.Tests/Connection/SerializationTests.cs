// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Assent;
using FluentAssertions;
using FluentAssertions.Equivalency;
using Microsoft.AspNetCore.Html;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests.Connection;

[Trait("Category", "Contracts and serialization")]
public class SerializationTests
{
    private readonly ITestOutputHelper _output;

    public SerializationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public void All_command_types_are_round_trip_serializable(KernelCommand command)
    {
        var originalEnvelope = KernelCommandEnvelope.Create(command);

        var json = KernelCommandEnvelope.Serialize(originalEnvelope);

        _output.WriteLine(json);

        var deserializedEnvelope = KernelCommandEnvelope.Deserialize(json);

        deserializedEnvelope
            .Should()
            .BeEquivalentToRespectingRuntimeTypes(
                originalEnvelope,
                o => o.Excluding(e => e.Command.Handler)
                      .Excluding(info => IsPropertyJsonIgnored(info)));
    }

    [Theory]
    [MemberData(nameof(Events))]
    public void All_event_types_are_round_trip_serializable(KernelEvent @event)
    {
        var originalEnvelope = KernelEventEnvelope.Create(@event);

        var json = KernelEventEnvelope.Serialize(originalEnvelope);

        _output.WriteLine($"{Environment.NewLine}{@event.GetType().Name}: {Environment.NewLine}{json}");

        var deserializedEnvelope = KernelEventEnvelope.Deserialize(json);

        deserializedEnvelope
            .Should()
            .BeEquivalentToRespectingRuntimeTypes(
                originalEnvelope,
                o => o.Excluding(info => IsPropertyJsonIgnored(info)));
    }

    private static bool IsPropertyJsonIgnored(IMemberInfo info)
    {
        var property = info.DeclaringType.GetProperty(info.Name);

        if (property is not null)
        {
            var jsonIgnore = property.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), true);
            if (jsonIgnore.Length> 0)
            {
                return true;
            }

        }

        return false;
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public void Command_contract_has_not_been_broken(KernelCommand command)
    {
        var _configuration = new Configuration()
            .UsingExtension($"{command.GetType().Name}.json")
            .SetInteractive(Debugger.IsAttached);

        command.SetToken("the-token");

        var json = KernelCommandEnvelope.Serialize(command);

        this.Assent(Indent(json), _configuration);
    }

    [Theory]
    [MemberData(nameof(EventsUniqueByType))]
    public void Event_contract_has_not_been_broken(KernelEvent @event)
    {
        var configuration = new Configuration()
            .UsingExtension($"{@event.GetType().Name}.json")
            .SetInteractive(Debugger.IsAttached);

        @event.Command.SetToken("the-token");

        var json = KernelEventEnvelope.Serialize(@event);

        this.Assent(Indent(json), configuration);
    }

    [Fact]
    public void All_command_types_are_tested_for_round_trip_serialization()
    {
        var commandTypes = new[]
                           {
                               typeof(Kernel),
                               typeof(CSharpKernel),
                               typeof(FSharpKernel),
                               typeof(HttpKernel),
                               typeof(PowerShellKernel),
                           }
                           .Select(t => t.Assembly)
                           .SelectMany(a => a.ExportedTypes);

        var interactiveCommands = commandTypes
                                  .Concrete()
                                  .DerivedFrom(typeof(KernelCommand));

        Commands()
            .Select(e => e[0].GetType())
            .Distinct()
            .Should()
            .BeEquivalentTo(interactiveCommands);
    }

    [Fact]
    public void All_event_types_are_tested_for_round_trip_serialization()
    {
        var interactiveEvents = typeof(Kernel)
            .Assembly
            .ExportedTypes
            .Concrete()
            .DerivedFrom(typeof(KernelEvent));

        Events()
            .Select(e => e[0].GetType())
            .Distinct()
            .Should()
            .BeEquivalentTo(interactiveEvents);
    }

    public static IEnumerable<object[]> Commands()
    {
        foreach (var command in commands().Select(c =>
                 {
                     c.RoutingSlip.StampAsArrived(new Uri("kernel://somelocation/kernelName", UriKind.Absolute));
                     c.RoutingSlip.Stamp(new Uri("kernel://somelocation/kernelName", UriKind.Absolute));
                     return c;
                 }))
        {
            yield return new object[] { command };
        }

        IEnumerable<KernelCommand> commands()
        {
            yield return new AddPackage("Microsoft.DotNet.Interactive", "*-*");

            yield return new AddPackageSource("https://api.nuget.org/v3/index.json");

            yield return new ClearValues();

            yield return new DisplayError("oops!");

            yield return new DisplayValue(
                new FormattedValue("text/html", "<b>hi!</b>")
            );

            yield return new ImportDocument(@"c:\temp\some.ipynb");

            yield return new RequestCompletions("Cons", new LinePosition(0, 4), "csharp");

            yield return new RequestDiagnostics("the-code");

            yield return new RequestHoverText("document-contents", new LinePosition(1, 2));

            yield return new RequestInput(prompt: "provide answer", inputTypeHint: "password");

            yield return new RequestInputs
            {
                Inputs =
                [
                    new InputDescription("value", "Please enter a value.")
                    {
                        TypeHint = "password",
                        SaveAs = "the-password"
                    }
                ]
            };

            yield return new RequestSignatureHelp("sig-help-contents", new LinePosition(1, 2));

            yield return new SendEditableCode("someKernelName", "code");

            yield return new SubmitCode("123", "csharp")
            {
                Parameters =
                {
                    ["SomeValue"] = "123"
                }
            };

            yield return new UpdateDisplayedValue(
                new FormattedValue("text/html", "<b>hi!</b>"),
                "the-value-id");

            yield return new Quit();

            yield return new Cancel("csharp");

            yield return new RequestKernelInfo(new Uri("kernel://webview/javascript"))
            {
                OriginUri = new("kernel://pid-1234/csharp")
            };

            yield return new RequestValueInfos("csharp");

            yield return new RequestValue("a", mimeType: HtmlFormatter.MimeType, targetKernelName: "csharp");

            yield return new SendValue(
                "name",
                "formatted value",
                new FormattedValue("text/plain", "formatted value"),
                targetKernelName: "fsharp");
        }
    }

    public static IEnumerable<object[]> Events()
    {
        foreach (var @event in events().Select(e =>
                 {
                     if (e is not KernelReady)
                     {
                         e.Command.RoutingSlip.StampAsArrived(new Uri("kernel://somelocation/kernelName"));
                         e.Command.RoutingSlip.Stamp(new Uri("kernel://somelocation/kernelName"));
                     }

                     e.RoutingSlip.Stamp(new Uri("kernel://somelocation/kernelName"));
                     return e;
                 }))
        {
            yield return new object[] { @event };
        }

        IEnumerable<KernelEvent> events()
        {
            yield return new CodeSubmissionReceived(
                new SubmitCode("123"));

            yield return new CommandFailed(
                "Oooops!",
                new SubmitCode("123"),
                executionOrder: 123);

            yield return new CommandFailed(
                new InvalidOperationException("Oooops!"),
                new SubmitCode("123"),
                "oops",
                executionOrder: 123);

            yield return new CommandSucceeded(new SubmitCode("123"), executionOrder: 123);

            yield return new CompleteCodeSubmissionReceived(new SubmitCode("123"));

            var requestCompletion = new RequestCompletions("Console.Wri", new LinePosition(0, 11));

            yield return new CompletionsProduced(
                new[]
                {
                    new CompletionItem(
                        displayText: "WriteLine",
                        kind: "Method",
                        filterText: "WriteLine",
                        sortText: "WriteLine",
                        insertText: "WriteLine",
                        insertTextFormat: InsertTextFormat.Snippet,
                        documentation: "Writes the line")
                },
                requestCompletion);

            var diagnostic = new Diagnostic(
                new LinePositionSpan(
                    new LinePosition(1, 2),
                    new LinePosition(3, 4)),
                DiagnosticSeverity.Error,
                "code",
                "message");

            yield return new DiagnosticsProduced(
                [diagnostic],
                [new FormattedValue(PlainTextFormatter.MimeType, diagnostic.ToString())],
                new SubmitCode("123"));

            yield return new DisplayedValueProduced(
                new HtmlString("<b>hi!</b>"),
                new SubmitCode("b(\"hi!\")", "csharp"),
                new[]
                {
                    new FormattedValue("text/html", "<b>hi!</b>"),
                });

            yield return new DisplayedValueUpdated(
                new HtmlString("<b>hi!</b>"),
                "the-value-id",
                new SubmitCode("b(\"hi!\")", "csharp"),
                new[]
                {
                    new FormattedValue("text/html", "<b>hi!</b>"),
                });

            yield return new ErrorProduced("oops!", new SubmitCode("123"));

            yield return new IncompleteCodeSubmissionReceived(new SubmitCode("123"));

            var requestHoverTextCommand = new RequestHoverText("document-contents", new LinePosition(1, 2));

            yield return new HoverTextProduced(
                requestHoverTextCommand,
                new[] { new FormattedValue("text/markdown", "markdown") },
                new LinePositionSpan(new LinePosition(1, 2), new LinePosition(3, 4)));

            yield return new KernelInfoProduced(
                new KernelInfo("javascript", aliases: new[] { "js" })
                {
                    LanguageName = "JavaScript",
                    Uri = new Uri("kernel://vscode/javascript"),
                    SupportedKernelCommands =
                    {
                        new KernelCommandInfo(nameof(SubmitCode))
                    },
                    SupportedDirectives =
                    {
                        new KernelActionDirective("#!example")
                        {
                            Parameters =
                            {
                                new("--opt")
                                {
                                    Required = true,
                                    TypeHint = "file",
                                    MaxOccurrences = 123
                                }
                            }
                        }
                    }
                },
                new RequestKernelInfo(new Uri("kernel://webview/javascript"))
                {
                    OriginUri = new("kernel://pid-1234/csharp")
                });

            yield return new KernelReady([
                new KernelInfo("javascript", aliases: new[] { "js" })
                {
                    LanguageName = "JavaScript",
                    Uri = new Uri("kernel://vscode/javascript"),
                    SupportedKernelCommands =
                    {
                        new KernelCommandInfo(nameof(SubmitCode))
                    }
                },
                new KernelInfo("csharp", aliases: new[] { "cs" })
                {
                    LanguageName = "CSharp",
                    Uri = new Uri("kernel://pid/csharp"),
                    Description = "executes C# code",
                    SupportedDirectives =
                    {
                        new KernelActionDirective("#r")
                    },
                    SupportedKernelCommands =
                    {
                        new KernelCommandInfo(nameof(SubmitCode))
                    }
                }
            ]);

            yield return new PackageAdded(
                new ResolvedPackageReference(
                    packageName: "ThePackage",
                    packageVersion: "1.2.3",
                    assemblyPaths: new[] { "/path/to/a.dll" },
                    packageRoot: "/the/package/root",
                    probingPaths: new[] { "/probing/path/1", "/probing/path/2" }),
                new SubmitCode("#r \"nuget:ThePackage,1.2.3\""));

            yield return new ReturnValueProduced(
                new HtmlString("<b>hi!</b>"),
                new SubmitCode("b(\"hi!\")", "csharp"),
                new[]
                {
                    new FormattedValue("text/html", "<b>hi!</b>"),
                });

            yield return new SignatureHelpProduced(
                new RequestSignatureHelp("sig-help-contents", new LinePosition(1, 2)),
                new[]
                {
                    new SignatureInformation("label",
                        new FormattedValue("text/html", "sig-help-result"),
                        new[]
                        {
                            new ParameterInformation("param1", new FormattedValue("text/html", "param1")),
                            new ParameterInformation("param2", new FormattedValue("text/html", "param2"))
                        })
                },
                0,
                1);

            yield return new StandardErrorValueProduced(
                new SubmitCode("123"),
                new[]
                {
                    new FormattedValue("text/plain", "oops!")
                });

            yield return new StandardOutputValueProduced(
                new SubmitCode("Console.Write(123);", "csharp"),
                new[]
                {
                    new FormattedValue("text/plain", "123")
                });

            yield return new KernelExtensionLoaded(new SubmitCode(@"#r ""nuget:package"" "));

            yield return new ValueInfosProduced(new[]
            {
                new KernelValueInfo("a",new FormattedValue(PlainTextSummaryFormatter.MimeType, "a"), typeof(string)),
                new KernelValueInfo("b",new FormattedValue(PlainTextSummaryFormatter.MimeType, "b"), typeof(List<string>)),
                new KernelValueInfo("c",new FormattedValue(PlainTextSummaryFormatter.MimeType, "c"), typeof(string[]))
            }, new RequestValueInfos("csharp"));

            yield return new ValueProduced(
                "raw value",
                "a",
                new FormattedValue(
                    HtmlFormatter.MimeType,
                    "<span>raw value</span>"),
                new RequestValue("a", mimeType: HtmlFormatter.MimeType, targetKernelName: "csharp"));

            yield return new InputProduced("user input", new RequestInput(prompt: "What is the path to the log file?", inputTypeHint: "file")
            {
                ParameterName = "filePath",
                SaveAs = "the-file"
            });

            yield return new InputsProduced(
                new Dictionary<string, string> { ["value"] = "tops3kr1tstuff" },
                new RequestInputs
                {
                    Inputs =
                    [
                        new InputDescription("value", "Please enter a value.")
                        {
                            TypeHint = "password",
                            SaveAs = "the-password"
                        }
                    ]
                });
        }
    }

    public static IEnumerable<object[]> EventsUniqueByType()
    {
        var dictionary = new Dictionary<Type, KernelEvent>();

        foreach (var e in Events().SelectMany(e => e).OfType<KernelEvent>())
        {
            dictionary[e.GetType()] = e;
        }

        return dictionary.Values.Select(e => new[] { e });
    }

    private static string Indent(string json)
    {
        json = JsonNode.Parse(json).ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        return json;
    }
}