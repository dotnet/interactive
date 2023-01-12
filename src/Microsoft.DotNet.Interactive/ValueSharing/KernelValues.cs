// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.ValueSharing;

[TypeFormatterSource(typeof(KernelValuesFormatterSource))]
internal class KernelValues : IEnumerable<KernelValue>
{
    private readonly Dictionary<string, KernelValue> _variables = new();

    public KernelValues(IEnumerable<KernelValue> variables, bool detailed)
        : this(detailed)
    {
        if (variables is null)
        {
            throw new ArgumentNullException(nameof(variables));
        }

        foreach (var variable in variables.Where(v => v is not null))
        {
            _variables[variable.Name] = variable;
        }
    }

    private KernelValues(bool detailed)
    {
        Detailed = detailed;
    }

    public bool Detailed { get; }

    public IEnumerator<KernelValue> GetEnumerator() => _variables.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}