// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Assent;
using FluentAssertions;
using Microsoft.AspNetCore.Html;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests.Connection
{
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
                    o => o.Excluding(e => e.Command.Properties)
                          .Excluding(e => e.Command.Handler));
        }

        [Theory]
        [MemberData(nameof(Events))]
        public void All_event_types_are_round_trip_serializable(KernelEvent @event)
        {
            var originalEnvelope = KernelEventEnvelope.Create(@event);

            var json = KernelEventEnvelope.Serialize(originalEnvelope);

            _output.WriteLine($"{Environment.NewLine}{@event.GetType().Name}: {Environment.NewLine}{json}");

            var deserializedEnvelope = KernelEventEnvelope.Deserialize(json);

            // ignore these specific properties because they're not serialized
            var ignoredProperties = new HashSet<string>
            {
                $"{nameof(CommandFailed)}.{nameof(CommandFailed.Exception)}",
                $"{nameof(DisplayEvent)}.{nameof(DisplayEvent.Value)}",
                $"{nameof(ValueProduced)}.{nameof(ValueProduced.Value)}",
                $"{nameof(KernelValueInfo)}.{nameof(KernelValueInfo.Type)}",
                $"{nameof(CommandCancelled)}.{nameof(CommandCancelled.CancelledCommand)}"
            };

            deserializedEnvelope
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(
                    originalEnvelope,
                    o => o.Excluding(envelope => envelope.Event.Command.Properties)
                        .Excluding(info => ignoredProperties.Contains($"{info.DeclaringType.Name}.{info.Name}"))
                    );
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

            @event.Command?.SetToken("the-token");

            var json = KernelEventEnvelope.Serialize(@event);

            this.Assent(Indent(json), configuration);
        }

        [Fact]
        public void All_command_types_are_tested_for_round_trip_serialization()
        {
            var interactiveCommands = typeof(Kernel)
                                      .Assembly
                                      .ExportedTypes
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
                c.Properties["id"] = "command-id";
                return c;
            }))
            {
                yield return new object[] { command };
            }

            IEnumerable<KernelCommand> commands()
            {
                yield return new AddPackage(new PackageReference("MyAwesomePackage", "1.2.3"));

                yield return new ChangeWorkingDirectory("/path/to/somewhere");

                yield return new DisplayError("oops!");

                yield return new DisplayValue(
                    new FormattedValue("text/html", "<b>hi!</b>")
                );

                yield return new RequestCompletions("Cons", new LinePosition(0, 4), "csharp");

                yield return new RequestDiagnostics("the-code");

                yield return new RequestHoverText("document-contents", new LinePosition(1, 2));

                yield return new RequestSignatureHelp("sig-help-contents", new LinePosition(1, 2));

                yield return new SendEditableCode("language", "code");

                yield return new SubmitCode("123", "csharp", SubmissionType.Run);

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

                yield return new RequestValue("a", "csharp", HtmlFormatter.MimeType);

                yield return new RequestInput(prompt:"provide answer", isPassword: true, targetKernelName: "vscode");
            }
        }

        public static IEnumerable<object[]> Events()
        {
            foreach (var @event in events().Select(e =>
            {
                e.Command.Properties["id"] = "command-id";
                return e;
            }))
            {
                yield return new object[] { @event };
            }

            IEnumerable<KernelEvent> events()
            {
                var submitCode = new SubmitCode("123");

                yield return new CodeSubmissionReceived(
                    submitCode);

                yield return new CommandFailed(
                    "Oooops!",
                    submitCode);

                yield return new CommandFailed(
                   new InvalidOperationException("Oooops!"),
                   submitCode,
                   "oops");

                yield return new CommandSucceeded(submitCode);

                yield return new CompleteCodeSubmissionReceived(submitCode);

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

                yield return new DiagnosticLogEntryProduced("oops!", submitCode);

                yield return new DiagnosticsProduced(
                    new[]
                    {
                        new Diagnostic(
                            new LinePositionSpan(
                                new LinePosition(1, 2),
                                new LinePosition(3, 4)),
                            DiagnosticSeverity.Error,
                            "code",
                            "message")
                    },
                    submitCode);

                yield return new DisplayedValueProduced(
                    new HtmlString("<b>hi!</b>"),
                    new SubmitCode("b(\"hi!\")", "csharp", SubmissionType.Run),
                    new[]
                    {
                        new FormattedValue("text/html", "<b>hi!</b>"),
                    });

                yield return new DisplayedValueUpdated(
                    new HtmlString("<b>hi!</b>"),
                    "the-value-id",
                    new SubmitCode("b(\"hi!\")", "csharp", SubmissionType.Run),
                    new[]
                    {
                        new FormattedValue("text/html", "<b>hi!</b>"),
                    });

                yield return new ErrorProduced("oops!", submitCode);

                yield return new IncompleteCodeSubmissionReceived(submitCode);

                var requestHoverTextCommand = new RequestHoverText("document-contents", new LinePosition(1, 2));

                yield return new HoverTextProduced(
                    requestHoverTextCommand,
                    new[] { new FormattedValue("text/markdown", "markdown") },
                    new LinePositionSpan(new LinePosition(1, 2), new LinePosition(3, 4)));

                yield return new KernelInfoProduced(
                    new KernelInfo("javascript", "JavaScript", aliases: new[] { "js" })
                    {
                        Uri = new Uri("kernel://vscode/javascript"),
                        SupportedDirectives = new[]
                        {
                            new KernelDirectiveInfo("#r")
                        },
                        SupportedKernelCommands = new[]
                        {
                            new KernelCommandInfo(nameof(SubmitCode))
                        }
                    },
                    new RequestKernelInfo(new Uri("kernel://webview/javascript"))
                    {
                        OriginUri = new("kernel://pid-1234/csharp")
                    });

                yield return new KernelReady();

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
                    new SubmitCode("b(\"hi!\")", "csharp", SubmissionType.Run),
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
                    submitCode,
                    new[]
                    {
                        new FormattedValue("text/plain", "oops!"),
                    });

                yield return new StandardOutputValueProduced(
                    new SubmitCode("Console.Write(123);", "csharp", SubmissionType.Run),
                    new[]
                    {
                        new FormattedValue("text/plain", "123"),
                    });

                yield return new WorkingDirectoryChanged(
                    "some/different/directory",
                    new ChangeWorkingDirectory("some/different/directory"));

                yield return new KernelExtensionLoaded(new SubmitCode(@"#r ""nuget:package"" "));

                yield return new ValueInfosProduced(new[] { new KernelValueInfo("a", typeof(string)), new KernelValueInfo("b", typeof(string)), new KernelValueInfo("c", typeof(string)) }, new RequestValueInfos("csharp"));

                yield return new ValueProduced("raw value", "a", new FormattedValue(HtmlFormatter.MimeType, "<span>formatted value</span>"), new RequestValue("a", "csharp", HtmlFormatter.MimeType));

                yield return new CommandCancelled( new Cancel() ,new SubmitCode("var value = 1;", "csharp"));

                yield return new InputProduced("user input", new RequestInput("Input?", targetKernelName: "vscode"));
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
                WriteIndented = true
            });
            return json;
        }
    }
}
