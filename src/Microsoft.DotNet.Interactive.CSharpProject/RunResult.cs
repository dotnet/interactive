// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.CSharpProject;

[JsonConverter(typeof(RunResultJsonConverter))]
public class RunResult : FeatureContainer
{
    private readonly List<string> _output = new(); 

    public RunResult(
        bool succeeded,
        IReadOnlyCollection<string> output = null,
        string exception = null,
        IEnumerable<SerializableDiagnostic> diagnostics = null, 
        string requestId = null)
    {
        if (output is not null)
        {
            _output.AddRange(output);
        }

        RequestId = requestId;
        Succeeded = succeeded;
        Exception = exception;
        AddFeature(new Diagnostics(diagnostics?.ToList() ??
                                   Array.Empty<SerializableDiagnostic>().ToList()));
    }

    public string RequestId { get; }
    public bool Succeeded { get; }

    public IReadOnlyCollection<string> Output => _output;

    public string Exception { get; }

    public override string ToString() =>
        $@"{nameof(Succeeded)}: {Succeeded}
{nameof(Output)}: {string.Join("\n", Output)}
{nameof(Exception)}: {Exception}";

    private class RunResultJsonConverter : FeatureContainerConverter<RunResult>
    {
        protected override void AddProperties(RunResult result, JObject o)
        {
            if (result.RequestId != null)
            {
                o.Add(new JProperty("requestId", result.RequestId));
            }
            o.Add(new JProperty("succeeded", result.Succeeded));
            o.Add(new JProperty("output", result.Output));
            o.Add(new JProperty("exception", result.Exception));
        }
    }
}