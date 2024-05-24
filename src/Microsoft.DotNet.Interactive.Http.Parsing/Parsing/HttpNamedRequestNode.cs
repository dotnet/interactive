using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing
{
    internal class HttpNamedRequestNode : HttpRequestNode
    {
        internal HttpNamedRequestNode(SourceText sourceText, HttpSyntaxTree syntaxTree) : base(sourceText, syntaxTree)
        {
        }

        public string Name { get; private set; }

        private HttpRequestNode RequestNode;

        private HttpResponse Response;
    }
}
