using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing.Parsing
{
    internal class DeclaredVariable
    {
        public string Name { get; }
        public string Value { get; }

        public HttpBindingResult<string> HttpBindingResult { get; }
        public DeclaredVariable(string name, string value, HttpBindingResult<string> httpBindingResult)
        {
            Name = name;
            Value = value;
            HttpBindingResult = httpBindingResult;
        }
    }
}
