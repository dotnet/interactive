// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Reflection;
using System.Text.Json;


namespace Microsoft.DotNet.Interactive.AIUtilities;

public class GPTFunctionDefinition
{
    public string Name { get; }
    private readonly Delegate _function;
    public string Signature { get; }

    private GPTFunctionDefinition(Delegate function, string signature, string name)
    {
        Name = name;
        _function = function;
        Signature = signature;
    }

    public void Execute<T>(string parameterJson, out T? result)
    {
        // parameters extraction
        var parameters = ExtractParameters(parameterJson);
        result = (T?)_function.DynamicInvoke(parameters);
    }

    private object?[]? ExtractParameters(string parameterJson)
    {
        var json = JsonDocument.Parse(parameterJson).RootElement;
        var parameterInfos = _function.Method.GetParameters();
        var parameters = new object?[parameterInfos.Length];
        var jsonArgs = JsonDocument.Parse( json.GetProperty("arguments").GetString() ).RootElement;
        for (var i = 0; i < parameterInfos.Length; i++)
        {
            parameters[i] = Deserialize(parameterInfos[i], jsonArgs);
        }
        
        return parameters;
    }

    private object? Deserialize(ParameterInfo parameterInfo, JsonElement jsonArgs)
    {
        if (jsonArgs.TryGetProperty(parameterInfo.Name, out var prop))
        {
            var arg = jsonArgs.GetProperty(parameterInfo.Name);
            return arg.Deserialize(parameterInfo.ParameterType);
        }
        else
        {
            if (parameterInfo.HasDefaultValue)
            {
                return parameterInfo.DefaultValue;
            }

            throw new ArgumentException($"The argument {parameterInfo.Name} is missing.");
        }
    }

    public static GPTFunctionDefinition Create(Delegate function, string name)
    {
        return new GPTFunctionDefinition(function, CreateSignature(function, name), name);
    }

    private static string CreateSignature(Delegate function, string name)
    {
        var parameters = function.Method.GetParameters();



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

        if (function.Method.ReturnType != typeof(void) )
        {
            call["results"] = GetType(function.Method.ReturnType);
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

            return "object";
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