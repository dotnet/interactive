// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.CSharpProject;

[JsonConverter(typeof(CompileResultJsonConverter))]
public class CompileResult : FeatureContainer
{
    public CompileResult(
        bool succeeded,
        string base64assembly,
        IEnumerable<SerializableDiagnostic> diagnostics = null,
        string requestId = null)
    {
        RequestId = requestId;
        Succeeded = succeeded;
        Base64Assembly = base64assembly;
        AddFeature(new Diagnostics(diagnostics?.ToList() ??
                                   Array.Empty<SerializableDiagnostic>().ToList()));
    }

    public bool Succeeded { get; }

    public string Base64Assembly { get; }

    public string RequestId { get;  }

    private class CompileResultJsonConverter : FeatureContainerConverter<CompileResult>
    {
        protected override void AddProperties(CompileResult result, JObject o)
        {
            if (result.RequestId is not null)
            {
                o.Add(new JProperty("requestId", result.RequestId));
            }
            o.Add(new JProperty("base64assembly", result.Base64Assembly));
            o.Add(new JProperty("succeeded", result.Succeeded));
        }
    }
}