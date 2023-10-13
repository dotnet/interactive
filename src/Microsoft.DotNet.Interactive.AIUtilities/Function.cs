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
            if (type == typeof(short)
                || type == typeof(int)
                || type == typeof(long)
                || type == typeof(ushort)
                || type == typeof(uint)
                || type == typeof(ulong)
                || type == typeof(byte)
                || type == typeof(sbyte)
                )
            {
                return "integer";
            }

            if (type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal))
            {
                return "number";
            }

            if (type == typeof(string))
            {
                return "string";
            }

            if (type == typeof(bool))
            {
                return "boolean";
            }

            throw new ArgumentException($"Invalid type {type}", nameof(type));
        }


        return JsonSerializer.Serialize(call, new JsonSerializerOptions(JsonSerializerOptions.Default) { WriteIndented = true });

        Dictionary<string, object> GetType(Type type)
        {
            var parameter = new Dictionary<string, object>();

            if (type.IsEnum)
            {
                var underlyingType = type.GetEnumUnderlyingType();
                parameter["type"] = GetTypeName(underlyingType);
                parameter["enum"] = GetEnumValues(type);
            }
            else if (type.IsArray)
            {
                parameter["type"] = "array";
                var elementType = type.GetElementType()!;
                if (elementType.IsEnum)
                {
                    var underlyingType = elementType.GetEnumUnderlyingType();

                    parameter["items"] = new Dictionary<string, object>
                    {
                        ["type"] = GetTypeName(underlyingType),
                        ["enum"] = GetEnumValues(elementType)
                    };
                }
                else
                {
                    parameter["items"] = new
                    {
                        type = GetTypeName(type.GetElementType()!),
                    };
                }
            }
            else
            {
                parameter["type"] = GetTypeName(type);
            }

            return parameter;
        }

        Array GetEnumValues(Type enumType)
        {
            return Enum.GetValues(enumType);
        }
    }
}