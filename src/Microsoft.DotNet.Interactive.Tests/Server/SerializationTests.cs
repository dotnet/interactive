// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Assent;
using FluentAssertions;
using Microsoft.AspNetCore.Html;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests.Server
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
                .BeEquivalentTo(originalEnvelope,
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

            deserializedEnvelope
                .Should()
                .BeEquivalentTo(originalEnvelope,
                                o => o.Excluding(envelope => envelope.Event.Command.Properties));
        }

        [Theory]
        [MemberData(nameof(Commands))]
        public void Command_contract_has_not_been_broken(KernelCommand command)
        {
            var _configuration = new Configuration()
                                 .UsingExtension($"{command.GetType().Name}.json")
                                 .SetInteractive(false);

            command.SetToken("the-token");

            var json = KernelCommandEnvelope.Serialize(command);

            this.Assent(json, _configuration);
        }

        [Theory]
        [MemberData(nameof(EventsUniqueByType))]
        public void Event_contract_has_not_been_broken(KernelEvent @event)
        {
            var _configuration = new Configuration()
                                 .UsingExtension($"{@event.GetType().Name}.json")
                                 .SetInteractive(false);

            @event.Command?.SetToken("the-token");

            var json = KernelEventEnvelope.Serialize(@event);

            this.Assent(json, _configuration);
        }

        [Fact]
        public void All_command_types_are_tested_for_round_trip_serialization()
        {
            var commandTypes = typeof(KernelCommand)
                               .Assembly
                               .ExportedTypes
                               .Concrete()
                               .DerivedFrom(typeof(KernelCommand));

            Commands()
                .Select(e => e[0].GetType())
                .Distinct()
                .Should()
                .BeEquivalentTo(commandTypes);
        }

        [Fact]
        public void All_event_types_are_tested_for_round_trip_serialization()
        {
            var eventTypes = typeof(KernelEvent)
                             .Assembly
                             .ExportedTypes
                             .Concrete()
                             .DerivedFrom(typeof(KernelEvent));

            Events()
                .Select(e => e[0].GetType())
                .Distinct()
                .Should()
                .BeEquivalentTo(eventTypes);
        }

        public static IEnumerable<object[]> Commands()
        {
            foreach (var command in commands())
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

                yield return new SubmitCode("123", "csharp", SubmissionType.Run);

                yield return new UpdateDisplayedValue(
                    new FormattedValue("text/html", "<b>hi!</b>"),
                    "the-value-id");
            }
        }

        public static IEnumerable<object[]> Events()
        {
            foreach (var @event in events())
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
                            "WriteLine",
                            "Method",
                            "WriteLine",
                            "WriteLine",
                            "WriteLine",
                            "Writes the line")
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

                yield return new ErrorProduced("oops!");

                yield return new IncompleteCodeSubmissionReceived(submitCode);

                yield return new InputRequested("prompt", submitCode);

                var requestHoverTextCommand = new RequestHoverText("document-contents", new LinePosition(1, 2));

                yield return new HoverTextProduced(
                    requestHoverTextCommand,
                    new[] { new FormattedValue("text/markdown", "markdown") },
                    new LinePositionSpan(new LinePosition(1, 2), new LinePosition(3, 4)));

                yield return new KernelReady();

                yield return new PackageAdded(
                    new ResolvedPackageReference(
                        packageName: "ThePackage",
                        packageVersion: "1.2.3",
                        assemblyPaths: new[] { "/path/to/a.dll" },
                        packageRoot: "/the/package/root",
                        probingPaths: new[] { "/probing/path/1", "/probing/path/2" }));

                yield return new PasswordRequested("password", submitCode);

                yield return new ReturnValueProduced(
                    new HtmlString("<b>hi!</b>"),
                    new SubmitCode("b(\"hi!\")", "csharp", SubmissionType.Run),
                    new[]
                    {
                        new FormattedValue("text/html", "<b>hi!</b>"),
                    });

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

                yield return new KernelExtensionLoaded("customExtension", new SubmitCode(@"#r ""nuget:package"" "));
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
    }
}
