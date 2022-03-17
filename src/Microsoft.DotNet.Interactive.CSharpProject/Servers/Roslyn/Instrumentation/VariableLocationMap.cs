// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation
{
    public class VariableLocationMap 
    {
        public Dictionary<ISymbol, HashSet<VariableLocation>> Data;
        public VariableLocationMap()
        {
#pragma warning disable RS1024 // Compare symbols correctly
            Data = new Dictionary<ISymbol, HashSet<VariableLocation>>(comparer: SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly
        }

        public void AddLocations(ISymbol variable, IEnumerable<VariableLocation> locations)
        {
            if (!Data.ContainsKey(variable))
            {
                Data[variable] = new HashSet<VariableLocation>();
            }

            foreach (var location in locations)
            {
                Data[variable].Add(location);
            }
        }

        public string Serialize()
        {
            var strings = Data.Select(kv =>
            {
                var variable = kv.Key;
                return SerializeForKey(variable);
            });
            var joined = @"\""variableLocations\"": [" + strings.Join() + "]";
            return joined;
        }

        public string SerializeForKey(ISymbol key)
        {
            string varLocations = Data[key]
                .Select(locations => locations.Serialize())
                .Join();

            var declaringReference = key.DeclaringSyntaxReferences.First();
            var declaringReferenceSyntax = declaringReference.GetSyntax();
            var location = declaringReferenceSyntax.Span;

            if (declaringReferenceSyntax is VariableDeclaratorSyntax vds)
            {
                location = vds.Identifier.Span;
            }
            else if (declaringReferenceSyntax is ForEachStatementSyntax fes)
            {
                location = fes.Identifier.Span;
            }

            string output = $@"
{{
    \""name\"": \""{key.Name}\"",
    \""locations\"": [{varLocations}],
    \""declaredAt\"": {{
        \""start\"": {location.Start},
        \""end\"": {location.End}
    }}
}}
";
            return output;
        }
    }
}
