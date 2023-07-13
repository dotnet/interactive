using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest
{
    internal record ParsedVariable(ParseItem Name, ParseItem Value, string ExpandedValue)
    {
        public string VariableName => Name.Text.Substring(1);
    }
}
