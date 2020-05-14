// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Html;
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

        [Theory(Timeout = 45000)]
        [MemberData(nameof(Commands))]
        public void All_command_types_are_round_trip_serializable(IKernelCommand command)
        {
            var originalEnvelope = KernelCommandEnvelope.Create(command);

            var json = KernelCommandEnvelope.Serialize(originalEnvelope);

            _output.WriteLine(json);

            var deserializedEnvelope = KernelCommandEnvelope.Deserialize(json);

            deserializedEnvelope
                .Should()
                .BeEquivalentTo(originalEnvelope,
                                o => o.Excluding(e => e.Command.Properties));
        }

        [Theory(Timeout = 45000)]
        [MemberData(nameof(Events))]
        public void All_event_types_are_round_trip_serializable(IKernelEvent @event)
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

        [Fact(Timeout = 45000)]
        public void All_command_types_are_tested_for_round_trip_serialization()
        {
            var commandTypes = typeof(IKernelCommand)
                               .Assembly
                               .ExportedTypes
                               .Concrete()
                               .DerivedFrom(typeof(IKernelCommand));

            Commands()
                .Select(e => e[0].GetType())
                .Distinct()
                .Should()
                .BeEquivalentTo(commandTypes);
        }

        [Fact(Timeout = 45000)]
        public void All_event_types_are_tested_for_round_trip_serialization()
        {
            var eventTypes = typeof(IKernelEvent)
                             .Assembly
                             .ExportedTypes
                             .Concrete()
                             .DerivedFrom(typeof(IKernelEvent));

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

            IEnumerable<IKernelCommand> commands()
            {
                yield return new AddPackage(new PackageReference("MyAwesomePackage", "1.2.3"));

                yield return new CancelCurrentCommand();

                yield return new ChangeWorkingDirectory("some/different/directory");

                yield return new DisplayError("oops!");

                yield return new DisplayValue(
                    new HtmlString("<b>hi!</b>"),
                    new FormattedValue("text/html", "<b>hi!</b>")
                );

                yield return new RequestCompletion("Cons", new LinePosition(0, 4), "csharp");

                yield return new RequestDiagnostics();

                yield return new RequestHoverText("document-contents", new LinePosition(1, 2));

                yield return new SubmitCode("123", "csharp", SubmissionType.Run);

                yield return new UpdateDisplayedValue(
                    new HtmlString("<b>hi!</b>"),
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

            IEnumerable<IKernelEvent> events()
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

                yield return new CommandHandled(submitCode);

                yield return new CompleteCodeSubmissionReceived(submitCode);

                var requestCompletion = new RequestCompletion("Console.Wri", new LinePosition(0, 11));

                yield return new CompletionRequestCompleted(
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

                yield return new CompletionRequestReceived(requestCompletion);

                yield return new DiagnosticLogEntryProduced("oops!", submitCode);

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

                yield return new PackageAdded(
                    new ResolvedPackageReference("ThePackage", "1.2.3", new[] { new FileInfo(Path.GetTempFileName()) }));

                yield return new PasswordRequested("password", submitCode);

                yield return new ReturnValueProduced(
                    new HtmlString("<b>hi!</b>"),
                    new SubmitCode("b(\"hi!\")", "csharp", SubmissionType.Run),
                    new[]
                    {
                        new FormattedValue("text/html", "<b>hi!</b>"),
                    });

                yield return new StandardErrorValueProduced(
                    "oops!",
                    submitCode,
                    new[]
                    {
                        new FormattedValue("text/plain", "oops!"),
                    });

                yield return new StandardOutputValueProduced(
                    123,
                    new SubmitCode("Console.Write(123);", "csharp", SubmissionType.Run),
                    new[]
                    {
                        new FormattedValue("text/plain", "123"),
                    });

                yield return new WorkingDirectoryChanged(
                    new ChangeWorkingDirectory("some/different/directory"),
                    "some/different/directory");
            }
        }
    }
}
