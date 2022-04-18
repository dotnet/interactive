// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;

namespace Microsoft.DotNet.Interactive.CSharpProject.Models.Instrumentation
{
    public class ProgramStateAtPositionArray : IRunResultFeature
    {
        [JsonProperty("instrumentation")]
        public IReadOnlyCollection<ProgramStateAtPosition> ProgramStates { get; set; }

        public string Name => nameof(ProgramStateAtPositionArray);

        public ProgramStateAtPositionArray(IReadOnlyCollection<string> programStates)
        {
            ProgramStates = programStates.Select(JsonConvert.DeserializeObject<ProgramStateAtPosition>).ToArray();
        }

        public void Apply(FeatureContainer result)
        {
            result.AddProperty("instrumentation", ProgramStates);
        }
    }
}

