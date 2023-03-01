// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests.Instrumentation;

public class InstrumentedOutputExtractorTests
{
    private static string _sentinel = "6a2f74a2-f01d-423d-a40f-726aa7358a81";
    private readonly List<string> instrumentedProgramOutput = new List<string>
    {
        _sentinel,
        #region variableLocation
        @"
{
""variableLocations"": [
    {
        ""name"": ""b"",
        ""locations"": [
          {
            ""startLine"": 12,
            ""startColumn"": 16,
            ""endLine"": 12,
            ""endColumn"": 21
          }
        ],
        ""declaredAt"": {
          ""start"": 176,
          ""end"": 181
        }
    }
]
}",
        #endregion
        _sentinel + _sentinel,
        #region programState
        @"
{
      ""filePosition"": {
        ""line"": 12,
        ""character"": 12,
        ""file"": ""Program.cs""
      },
      ""stackTrace"": ""    at FibonacciTest.Program.Main()\r\n "",
      ""locals"": [
        {
          ""name"": ""a"",
          ""value"": ""4"",
          ""declaredAt"": {
            ""start"": 153,
            ""end"": 154
          }
        }
      ],
      ""parameters"": [],
      ""fields"": []
}
",
        #endregion
        _sentinel,
        "program output",
        _sentinel,
        #region programState
        @"
{
      ""filePosition"": {
        ""line"": 13,
        ""character"": 12,
        ""file"": ""Program.cs""
      },
      ""stackTrace"": ""    at FibonacciTest.Program.Main()\r\n "",
      ""locals"": [],
      ""parameters"": [{
          ""name"": ""p"",
          ""value"": ""1"",
          ""declaredAt"": {
            ""start"": 1,
            ""end"": 1
          }
        }],
      ""fields"": [{
          ""name"": ""f"",
          ""value"": ""2"",
          ""declaredAt"": {
            ""start"": 2,
            ""end"": 2
          }
        }]
}
",
        #endregion
        _sentinel,
        "blank",
        "",
        " ",
        " lines ",
        "even more output",
        _sentinel,
        #region programState
        @"
{
      ""filePosition"": {
        ""line"": 13,
        ""character"": 12,
        ""file"": ""Program.cs""
      },
      ""stackTrace"": ""    at FibonacciTest.Program.Main()\r\n "",
      ""locals"": [],
      ""parameters"": [{
          ""name"": ""p"",
          ""value"": ""1"",
          ""declaredAt"": {
            ""start"": 1,
            ""end"": 1
          }
        }],
      ""fields"": [{
          ""name"": ""f"",
          ""value"": ""2"",
          ""declaredAt"": {
            ""start"": 2,
            ""end"": 2
          }
        }]
}
",
        #endregion
        _sentinel
    };

    private readonly ProgramOutputStreams splitOutput;

    public InstrumentedOutputExtractorTests()
    {
        var normalizedOutput = instrumentedProgramOutput.Select(line => line.EnforceLF()).ToArray();
        splitOutput = InstrumentedOutputExtractor.ExtractOutput(normalizedOutput);
    }

    public class Non_Sentinel_Bounded_Strings_Are_Parsed_As_Output : InstrumentedOutputExtractorTests
    {
        [Fact]
        public void Standard_out_contains_comlpete_output_and_no_sentinels_or_program_metadata()
        {
            splitOutput.StdOut
                .Should()
                .BeEquivalentTo(new[]
                {
                    "program output",
                    "blank",
                    "",
                    " ",
                    " lines ",
                    "even more output",
                    ""
                }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void Empty_standard_out_remains_empty_after_extraction()
        {
            InstrumentedOutputExtractor.ExtractOutput(new string[] { })
                .StdOut.Count().Should().Be(0);
        }

        [Fact]
        public void Empty_standard_out_with_instrumentation_remains_empty_after_extraction()
        {
            InstrumentedOutputExtractor.ExtractOutput(
                new[] {
                    _sentinel,
                    #region variableLocation
                    @"
{
""variableLocations"": [
    {
        ""name"": ""b"",
        ""locations"": [
          {
            ""startLine"": 12,
            ""startColumn"": 16,
            ""endLine"": 12,
            ""endColumn"": 21
          }
        ],
        ""declaredAt"": {
          ""start"": 176,
          ""end"": 181
        }
    }
]
}",
                    #endregion
                    _sentinel + _sentinel,
                    #region programState
                    @"
{
      ""filePosition"": {
        ""line"": 12,
        ""character"": 12,
        ""file"": ""Program.cs""
      },
      ""stackTrace"": ""    at FibonacciTest.Program.Main()\r\n "",
      ""locals"": [
        {
          ""name"": ""a"",
          ""value"": ""4"",
          ""declaredAt"": {
            ""start"": 153,
            ""end"": 154
          }
        }
      ],
      ""parameters"": [],
      ""fields"": []
}
",
                    #endregion
                    _sentinel
                }
            ).StdOut.Count().Should().Be(0);
        }
    }

    public class First_Sentinel_Bounded_String_Is_Parsed_As_ProgramDescriptor : InstrumentedOutputExtractorTests
    {
        [Fact]
        public void It_Should_Have_Correct_Variable_Name()
        {
            splitOutput.ProgramDescriptor.VariableLocations.First().Name.Should().Be("b");
        }

        [Fact]
        public void It_Should_Have_Correct_Location()
        {
            splitOutput.ProgramDescriptor.VariableLocations.First().Locations.First().StartColumn.Should().Be(16);
        }
    }

    public class Rest_Of_Sentinel_Bounded_Strings_Are_Parsed_As_ProgramState : InstrumentedOutputExtractorTests
    {
        [Fact]
        public void First_Program_State_Has_Correct_Local_Name()
        {
            splitOutput.ProgramStatesArray.ProgramStates.First().Locals.First().Name.Should().Be("a");
        }

        [Fact]
        public void Second_Program_State_Has_Correct_Parameter_Name()
        {
            splitOutput.ProgramStatesArray.ProgramStates.ElementAt(1).Parameters.First().Name.Should().Be("p");
        }

        [Fact]
        public void Second_Program_State_Has_Correct_Field_Name()
        {
            splitOutput.ProgramStatesArray.ProgramStates.ElementAt(1).Fields.First().Name.Should().Be("f");
        }

        [Fact]
        public void Original_output_can_be_reconstructed_from_per_step_output_indices()
        {
            const string newline = "\n";
            var output = splitOutput.ProgramStatesArray.ProgramStates
                .Select(x => ((int)x.Output.Start, (int)x.Output.End))
                .Select(tuple => splitOutput.StdOut.Join("\n").Substring(tuple.Item1, tuple.Item2 - tuple.Item1))
                .Where(str => !string.IsNullOrEmpty(str));

            var firstEmittedLine = "program output" + newline;
            var secondEmittedLine =
                "blank" + newline +
                "" + newline +
                " " + newline +
                " lines " + newline +
                "even more output" + newline +
                "";

            output.Should().BeEquivalentTo(
                firstEmittedLine,
                secondEmittedLine
            );
        }
    }

}