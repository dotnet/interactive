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
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.DotNet.Interactive.CSharpProject.Events;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;
using Pocket;
using Xunit;
using Xunit.Abstractions;
using System.Text.Encodings.Web;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

[Trait("Category", "Contracts and serialization")]
public class SerializationTests
{
    private readonly ITestOutputHelper _output;

    public SerializationTests(ITestOutputHelper output)
    {
        _output = output;

        KernelCommandEnvelope.RegisterCommand<OpenProject>();
        KernelCommandEnvelope.RegisterCommand<OpenDocument>();
        KernelCommandEnvelope.RegisterCommand<CompileProject>();
        KernelEventEnvelope.RegisterEvent<ProjectOpened>();
        KernelEventEnvelope.RegisterEvent<DocumentOpened>();
        KernelEventEnvelope.RegisterEvent<AssemblyProduced>();
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
            .BeEquivalentToPreferringRuntimeMemberTypes(
                originalEnvelope,
                o => o.Excluding(e => e.Command.Handler));
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
        };

        deserializedEnvelope
            .Should()
            .BeEquivalentToPreferringRuntimeMemberTypes(
                originalEnvelope,
                o => o.Excluding(memberInfo => ignoredProperties.Contains($"{memberInfo.DeclaringType.Name}.{memberInfo.Name}"))
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
        var projectKernelCommands = typeof(CSharpProjectKernel)
            .Assembly
            .ExportedTypes
            .Concrete()
            .DerivedFrom(typeof(KernelCommand));

        Commands()
            .Select(e => e[0].GetType())
            .Distinct()
            .Should()
            .BeEquivalentTo(projectKernelCommands);
    }

    [Fact]
    public void All_event_types_are_tested_for_round_trip_serialization()
    {
        var projectKernelEvents = typeof(CSharpProjectKernel)
            .Assembly
            .ExportedTypes
            .Concrete()
            .DerivedFrom(typeof(KernelEvent));

        Events()
            .Select(e => e[0].GetType())
            .Distinct()
            .Should()
            .BeEquivalentTo(projectKernelEvents);
    }

    public static IEnumerable<object[]> Commands()
    {
        foreach (var command in commands().Select(c =>
                 {
                     c.RoutingSlip.StampAsArrived(new Uri("kernel://somelocation/kernelA"));
                     c.RoutingSlip.StampAsArrived(new Uri("kernel://somelocation/kernelName"));
                     c.RoutingSlip.Stamp(new Uri("kernel://somelocation/kernelName"));
                     return c;
                 }))
        {
            yield return new object[] { command };
        }

        IEnumerable<KernelCommand> commands()
        {
            yield return new CompileProject();
                
            yield return new OpenDocument("path");

            yield return new OpenProject(new Project(new[] { new ProjectFile("Program.cs", "// file contents") }));
        }
    }

    public static IEnumerable<object[]> Events()
    {
        foreach (var @event in events().Select(e =>
                 {
                     e.Command.RoutingSlip.StampAsArrived(new Uri("kernel://somelocation/kernelA"));
                     e.Command.RoutingSlip.StampAsArrived(new Uri("kernel://somelocation/kernelName"));
                     e.Command.RoutingSlip.Stamp(new Uri("kernel://somelocation/kernelName"));
                     e.RoutingSlip.Stamp(new Uri("kernel://somelocation/kernelName"));
                     return e;
                 }))
        {
            yield return new object[] { @event };
        }

        IEnumerable<KernelEvent> events()
        {
            var compileProject = new CompileProject();

            yield return new AssemblyProduced(compileProject, new Base64EncodedAssembly("01020304"));
                
            var openDocument = new OpenDocument("path");

            yield return new DocumentOpened(openDocument, new RelativeFilePath("path"), null, "file contents");

            yield return new ProjectOpened(
                new OpenProject(new Project(new[]
                {
                    new ProjectFile("Program.cs", "#region some-region\n#endregion"),
                })),
                new[]
                {
                    new ProjectItem("./Program.cs", new[] { "some-region" }, new Dictionary<string, string>{["some-region"] = string.Empty})
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