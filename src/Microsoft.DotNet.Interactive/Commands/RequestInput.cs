// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestInput : KernelCommand
{
    [JsonConstructor]
    public RequestInput(
        string prompt,
        string inputTypeHint = null)
    {
        Prompt = prompt;
        InputTypeHint = inputTypeHint;
    }

    public string ParameterName { get; set; }

    public string Prompt { get; private set; }

    public bool IsPassword => InputTypeHint is "password";

    public string SaveAs { get; set; }

    [JsonPropertyName("type")] 
    public string InputTypeHint { get; set; }

    internal static RequestInput Parse(DirectiveExpressionNode expressionNode)
    {
        if (expressionNode.ChildNodes.OfType<DirectiveExpressionTypeNode>().SingleOrDefault() is not { } expressionTypeNode)
        {
            throw new ArgumentException("Expression type not found");
        }

        var parametersNode = expressionNode.ChildNodes.OfType<DirectiveExpressionParametersNode>().SingleOrDefault();

        var expressionType = expressionTypeNode.Type;

        var parametersNodeText = parametersNode?.Text;

        string parameterName = "";

        if (expressionNode.Ancestors().OfType<DirectiveParameterNode>().FirstOrDefault() is { } parameterNode)
        {
            if (parameterNode.DescendantNodesAndTokens().OfType<DirectiveParameterNameNode>().FirstOrDefault() is { } parameterNameNode)
            {
                parameterName = parameterNameNode.Text;
            }
            else if (parameterNode.TryGetParameter(out var parameter) && 
                     parameter.AllowImplicitName)
            {
                parameterName = parameter.Name;
            }
        }

        RequestInput requestInput;

        if (string.IsNullOrWhiteSpace(parametersNodeText))
        {
            requestInput = new(prompt: $"Please enter a value for parameter: {parameterName}");
        }
        else if (parametersNodeText?[0] is '{')
        {
            requestInput = JsonSerializer.Deserialize<RequestInput>(parametersNode.Text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (string.IsNullOrWhiteSpace(requestInput.Prompt))
            {
                requestInput.Prompt = $"Please enter a value for parameter: {parameterName}";
            }
        }
        else
        {
            if (parametersNodeText?[0] is '"')
            {
                parametersNodeText = JsonSerializer.Deserialize<string>(parametersNode.Text);
            }

            if (parametersNodeText?.Contains(" ") is true)
            {
                requestInput = new(prompt: parametersNodeText);
            }
            else
            {
                requestInput = new(prompt: $"Please enter a value for parameter: {parameterName}");
            }
        }

        requestInput.ParameterName = parameterName;

        if (expressionType is "password")
        {
            requestInput.InputTypeHint = "password";
        }
        else if (string.IsNullOrEmpty(requestInput.InputTypeHint))
        {
            if (expressionNode.Parent?.Parent is DirectiveParameterNode parameterValueNode)
            {
                if (parameterValueNode.TryGetParameter(out var parameter) &&
                    parameter.TypeHint is { } typeHint)
                {
                    requestInput.InputTypeHint = typeHint;
                }
            }
        }

        return requestInput;
    }
}