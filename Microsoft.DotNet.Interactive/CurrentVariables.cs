// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive
{
    public class CurrentVariables : IEnumerable<CurrentVariable>
    {
        private readonly Dictionary<string, CurrentVariable> _variables = new Dictionary<string, CurrentVariable>();

        public CurrentVariables(IEnumerable<CurrentVariable> variables, bool detailed)
            : this(detailed)
        {
            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            foreach (var variable in variables.Where(v => v != null))
            {
                _variables[variable.Name] = variable;
            }
        }

        private CurrentVariables(bool detailed)
        {
            Detailed = detailed;
        }

        public bool Detailed { get; }

        public IEnumerator<CurrentVariable> GetEnumerator() => _variables.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
