// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation
{
    public class InstrumentationMap
    {
        public InstrumentationMap(string fileToInstrument, IEnumerable<Microsoft.CodeAnalysis.Text.TextSpan> instrumentationRegions)
        {
            FileToInstrument = fileToInstrument;
            InstrumentationRegions = instrumentationRegions ?? Array.Empty<Microsoft.CodeAnalysis.Text.TextSpan>();
        }

        public string FileToInstrument { get; }

        public IEnumerable<Microsoft.CodeAnalysis.Text.TextSpan> InstrumentationRegions { get; }
    }
}
