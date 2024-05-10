// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class Diagnostics : ReadOnlyCollection<SerializableDiagnostic>, IRunResultFeature
{
    public Diagnostics(IList<SerializableDiagnostic> list) : base(list.Where(d => d.Severity is not DiagnosticSeverity.Hidden).ToList())
    {
    }

    public string Name => nameof(Diagnostics);

    public void Apply(FeatureContainer result)
    {
        result.AddProperty("diagnostics", this.Sort());
    }
}