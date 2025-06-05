#nullable enable

using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;
internal class HttpEscapedCharacterSequenceNode : HttpSyntaxNode
{
    public HttpEscapedCharacterSequenceNode(SourceText sourceText, HttpSyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public string NonEscapedText => Text.TrimStart('\\');

}
