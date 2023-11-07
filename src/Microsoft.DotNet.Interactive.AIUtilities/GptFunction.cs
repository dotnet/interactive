// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pocket;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.AIUtilities;

public class GptFunction
{
    private static readonly JsonSerializerOptions SerializerOptions;
    private readonly Delegate _function;

    static GptFunction()
    {
        SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General);

        SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    internal GptFunction(string name, string jsonSignature, Delegate function, string? description)
    {
        Name = name;
        _function = function;
        JsonSignature = jsonSignature;
        Description = description;
    }

    public string? Description { get; }
    public string JsonSignature { get; }
    public string Name { get; }

    public object? Execute(string parameterJson)
    {
        Logger.Log.Event();
        // parameters extraction
        var json = JsonDocument.Parse(parameterJson).RootElement;
        if (json.TryGetProperty("arguments", out var args))
        {
            var argsString = args.ToString();
            if (string.IsNullOrWhiteSpace(argsString))
            {
                var parameterInfos = _function.Method.GetParameters();
                if (parameterInfos.Any(p => !p.IsOptional))
                {
                    throw new ArgumentException("no parameters defined.");
                }
            }
            var jsonArgs = JsonDocument.Parse(args.GetString()!).RootElement;
            return Execute(jsonArgs);
        }

        return Execute(json);
    }

    public object? Execute(JsonElement json)
    {
        // parameters extraction
        var parameters = ExtractParameters(json);
        return _function.DynamicInvoke(parameters);
    }

    private object?[] ExtractParameters(JsonElement json)
    {

        var parameterInfos = _function.Method.GetParameters();
        var parameters = new object?[parameterInfos.Length];
        for (var i = 0; i < parameterInfos.Length; i++)
        {
            parameters[i] = Deserialize(parameterInfos[i], json);
        }

        return parameters;
    }

    private object? Deserialize(ParameterInfo parameterInfo, JsonElement jsonArgs)
    {
        if (jsonArgs.TryGetProperty(parameterInfo.Name!, out var arg))
        {
           return arg.Deserialize(parameterInfo.ParameterType, SerializerOptions);
        }

        if (parameterInfo.HasDefaultValue)
        {
            return parameterInfo.DefaultValue;
        }

        throw new ArgumentException($"The argument {parameterInfo.Name} is missing.");
    }

    public static GptFunction Create(string name, Delegate function, string? description = null, bool enumsAsString = false)
    {
        Logger.Log.Event();
        return new GptFunction(name, CreateSignature(function, name, enumsAsString), function, description);
    }

    private static string CreateSignature(Delegate function, string name, bool enumsAsString)
    {
        var parameters = function.Method.GetParameters();

        var requiredParameters = parameters.Where(p => !p.HasDefaultValue).Select(p => p.Name).ToArray();
        var signature = new Dictionary<string, object>
        {
            ["name"] = name,
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = GetParameters(parameters)
            }
        };

        if (function.Method.ReturnType != typeof(void))
        {
            signature["results"] = GetType(function.Method.ReturnType);
        }


        if (requiredParameters.Length > 0)
        {
            signature["required"] = requiredParameters;
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

        return JsonSerializer.Serialize(signature);

        Dictionary<string, object> GetType(Type type)
        {
            var parameter = new Dictionary<string, object>();

            if (type.IsEnum)
            {
                var underlyingType = type.GetEnumUnderlyingType();
                if (!enumsAsString)
                {
                    parameter["type"] = GetTypeName(underlyingType);
                    parameter["enum"] = GetEnumValues(type);
                }
                else
                {
                    parameter["type"] = "string";
                    parameter["enum"] = Enum.GetNames(type);
                }
            }
            else if (type.IsArray)
            {
                parameter["type"] = "array";
                var elementType = type.GetElementType()!;
                if (elementType.IsEnum)
                {
                    var underlyingType = elementType.GetEnumUnderlyingType();
                    if (!enumsAsString)
                    {
                        parameter["items"] = new Dictionary<string, object>
                        {
                            ["type"] = GetTypeName(underlyingType),
                            ["enum"] = GetEnumValues(elementType)
                        };

                    }
                    else
                    {
                        parameter["items"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["enum"] = Enum.GetNames(elementType)
                    };
                    }
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