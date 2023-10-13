// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.AIUtilities;

public static class GPTFunctioDefinition
{
    public static string Do(Delegate d, string name)
    {
        var parameters = d.Method.GetParameters();

       

        var requiredParameters = parameters.Where(p => !p.HasDefaultValue).Select(p => p.Name).ToArray();
        var call = new Dictionary<string, object>
        {
            ["name"] = name,
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = GetParameters(parameters)
            }
        };

        if (d.Method.ReturnType is not null)
        {
            call["results"] = GetType(d.Method.ReturnType);
        }


        if (requiredParameters.Length > 0)
        {
            call["required"] = requiredParameters;
        }

        Dictionary<string, object> GetParameters(ParameterInfo[] parameterInfos)
        {
            var signature = new Dictionary<string, object>();

            foreach (var parameterInfo in parameterInfos)
            {
                var parameter = GetType(parameterInfo.ParameterType);

                signature[parameterInfo.Name!] = parameter;
            }

            return signature;
        }

        static string GetTypeName(Type type)
        {
            if (type == typeof(int))
            {
                return "number";
            }

            if (type == typeof(long))
            {
                return  "number";
            }

            if (type == typeof(string))
            {
                return "string";
            }

            throw new ArgumentException($"Invalid type {type}", nameof(type));
        }


        return JsonSerializer.Serialize(call, new JsonSerializerOptions(JsonSerializerOptions.Default){ WriteIndented = true});

        Dictionary<string, object> GetType(Type type)
        {
            var parameter = new Dictionary<string, object>();

            if (type.IsArray)
            {
                parameter["type"] = "array";
                parameter["items"] = new
                {
                    type = GetTypeName(type.GetElementType()!),
                };
            }
            else if (type.IsEnum)
            {
                var underlyingType = type.GetEnumUnderlyingType();
            }
            else
            {
                parameter["type"] = GetTypeName(type);
            }

            return parameter;
        }
    }
}