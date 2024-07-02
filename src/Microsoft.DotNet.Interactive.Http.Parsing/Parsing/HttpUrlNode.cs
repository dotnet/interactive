// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

using Diagnostic = CodeAnalysis.Diagnostic;

internal class HttpUrlNode : HttpSyntaxNode
{
    internal HttpUrlNode(SourceText sourceText, HttpSyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public void Add(HttpEmbeddedExpressionNode node) => AddInternal(node);

    public override IEnumerable<Diagnostic> GetDiagnostics()
    {
        foreach (var diagnostic in base.GetDiagnostics())
        {
            yield return diagnostic;
        }

        if (!ChildNodes.OfType<HttpEmbeddedExpressionNode>().Any())
        {
            if (Uri.TryCreate(Text, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme is not "http" and not "https")
                {
                    yield return CreateDiagnostic(HttpDiagnostics.UnrecognizedUriScheme(uri.Scheme));
                }
            }
            else
            {
                yield return CreateDiagnostic(HttpDiagnostics.InvalidUri());
            }
        }
    }

    internal HttpBindingResult<Uri> TryGetUri(HttpBindingDelegate bind)
    {
        var result = this.BindByInterpolation(bind);

        if (result.IsSuccessful)
        {
            var uri = new Uri(result.Value!, UriKind.Absolute);

            return HttpBindingResult<Uri>.Success(uri, result.Diagnostics.ToArray());
        }
        else
        {
            return HttpBindingResult<Uri>.Failure(result.Diagnostics.ToArray());
        }
    }
}