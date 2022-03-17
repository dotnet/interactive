// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation
{
    internal class ExtractorState
    {
        public readonly bool IsInstrumentation;
        public readonly string ProgramDescriptor;
        public readonly ImmutableList<string> Instrumentation;
        public readonly ImmutableList<string> StdOut;

        public ExtractorState(
            bool isInstrumentation = false,
            string programDescriptor = "",
            ImmutableList<string> instrumentation = null,
            ImmutableList<string> stdOut = null)
        {
            IsInstrumentation = isInstrumentation;
            ProgramDescriptor = programDescriptor;
            Instrumentation = instrumentation ?? ImmutableList.Create<string>();
            StdOut = stdOut ?? ImmutableList.Create<string>();
        }

        public ExtractorState With(
            bool? isInstrumentation = null,
            string programDescriptor = "",
            ImmutableList<string> instrumentation = null,
            ImmutableList<string> stdOut = null)
        {
            return new ExtractorState(
                isInstrumentation ?? IsInstrumentation,
                programDescriptor == "" ? ProgramDescriptor : programDescriptor,
                instrumentation ?? Instrumentation,
                stdOut ?? StdOut);
        }
    }
}
