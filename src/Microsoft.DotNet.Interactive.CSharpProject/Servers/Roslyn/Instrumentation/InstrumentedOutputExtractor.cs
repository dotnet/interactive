// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Recipes;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;

public static class InstrumentedOutputExtractor
{
    private static readonly string _sentinel = "6a2f74a2-f01d-423d-a40f-726aa7358a81"; //TODO: get this from the syntax re-writer

    public static ProgramOutputStreams ExtractOutput(IReadOnlyCollection<string> outputLines)
    {
        if (outputLines == null || outputLines.Count == 0)
        {
            return new ProgramOutputStreams(outputLines, Array.Empty<string>());
        }

        var newLine = "\n";

        string rawOutput = string.Join(newLine, outputLines);

        var splitOutput = rawOutput
            .TokenizeWithDelimiter(_sentinel)
            .Aggregate(new ExtractorState(), (currentState, nextString) =>
            {
                if (nextString.TrimEnd() == _sentinel)
                {
                    return currentState.With(isInstrumentation: !currentState.IsInstrumentation);
                }

                if (currentState.IsInstrumentation)
                {
                    // First piece of instrumentation is always program descriptor
                    if (currentState.ProgramDescriptor == "")
                    {
                        return currentState.With(programDescriptor: nextString.Trim());
                    }
                    else
                    {
                        // Why do we need these indices? To figure out how much stdout to expose for
                        // every piece of instrumentation.
                        var (outputStart, outputEnd) = GetSpanOfStdOutCreatedAtCurrentStep(currentState);

                        var newOutput = new LineRange
                        {
                            Start = outputStart,
                            End = outputEnd
                        };

                        var modifiedInstrumentation = JsonConvert.DeserializeObject<ProgramStateAtPosition>(nextString.Trim());
                        modifiedInstrumentation.Output = newOutput;

                        return currentState.With(
                            instrumentation: currentState.Instrumentation.Add(modifiedInstrumentation.ToJson())
                        );
                    }
                }
                else
                {
                    return currentState.With(
                        stdOut: currentState.StdOut.Add(nextString)
                    );
                }
            });

        var withSplitStdOut = splitOutput.With(stdOut: SplitStdOutByNewline(splitOutput.StdOut));

        return new ProgramOutputStreams(withSplitStdOut.StdOut, withSplitStdOut.Instrumentation, withSplitStdOut.ProgramDescriptor);
    }

    static ImmutableList<string> SplitStdOutByNewline(ImmutableList<string> stdOut)
    {
        if (stdOut.IsEmpty)
        {
            return stdOut;
        }
        else
        {
            return stdOut
                .Join(String.Empty)
                .Split('\n')
                .ToImmutableList();
        }
    }

    static (int outputStart, int outputEnd) GetSpanOfStdOutCreatedAtCurrentStep(ExtractorState currentState)
    {
        if (currentState.StdOut.IsEmpty)
        {
            return (0, 0);
        }

        var newOutput = currentState.StdOut.Last();
        var entireOutput = currentState.StdOut.Join(String.Empty);
        var endLocation = entireOutput.Length;

        return (endLocation - newOutput.Length, endLocation);
    }

    static IEnumerable<string> TokenizeWithDelimiter(this string input, string delimiter) => Regex.Split(input, $"({delimiter}[\n]?)").Where(str => !String.IsNullOrWhiteSpace(str));

}