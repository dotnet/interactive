// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

public class InputDescription
{
    public InputDescription(string name, string prompt = null)
    {
        Name = name;
        Prompt = prompt ?? name;
    }

    public string Name { get; }

    public string Prompt { get; }

    public string SaveAs { get; set; }

    [JsonPropertyName("type")] 
    public string TypeHint { get; set; }

    internal DirectiveExpressionNode ExpressionNode { get; set; }

    internal static InputDescription Parse(DirectiveExpressionNode expressionNode)
    {
        var requestInput = RequestInput.Parse(expressionNode);

        return new InputDescription(requestInput.ParameterName, requestInput.Prompt)
        {
            ExpressionNode = expressionNode,
            SaveAs = requestInput.SaveAs,
            TypeHint = requestInput.InputTypeHint
        };
    }
}