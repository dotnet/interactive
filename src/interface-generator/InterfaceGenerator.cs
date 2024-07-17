// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.App.ParserServer;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharpProject;
using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.DotNet.Interactive.CSharpProject.Events;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.VSCode;

namespace Microsoft.DotNet.Interactive.InterfaceGen.App;

public class InterfaceGenerator
{
    private static readonly Dictionary<Type, string> WellKnownTypes = new()
    {
        { typeof(bool), "boolean" },
        { typeof(byte), "number" },
        { typeof(int), "number" },
        { typeof(object), "any" },
        { typeof(string), "string" },
        { typeof(byte[]), "Uint8Array" },

        { typeof(DirectoryInfo), "string" },
        { typeof(FileInfo), "string" },
        { typeof(Uri), "string" },
    };

    private static readonly Dictionary<Type, string> TypeNameOverrides = new()
    {
        { typeof(Documents.KernelInfo), "DocumentKernelInfo" }
    };

    private static readonly HashSet<Type> AlwaysEmitTypes = new()
    {
        typeof(KernelCommand),
        typeof(KernelEvent),
        typeof(DisplayElement),
        typeof(ReturnValueElement),
        typeof(TextElement),
        typeof(ErrorElement),
        typeof(Documents.KernelInfo),
    };

    private static readonly HashSet<Type> ParserServerTypes = new()
    {
        // requests
        typeof(NotebookParseRequest),
        typeof(NotebookSerializeRequest),

        // responses
        typeof(NotebookParseResponse),
        typeof(NotebookSerializeResponse),
    };

    private static readonly HashSet<string> OptionalFields = new()
    {
        $"{nameof(CompletionsProduced)}.{nameof(CompletionsProduced.LinePositionSpan)}",
        $"{nameof(DisplayEvent)}.{nameof(DisplayEvent.ValueId)}",
        $"{nameof(DocumentOpened)}.{nameof(DocumentOpened.RegionName)}",
        $"{nameof(HoverTextProduced)}.{nameof(HoverTextProduced.LinePositionSpan)}",
        $"{nameof(OpenDocument)}.{nameof(OpenDocument.RegionName)}",
        $"{nameof(SubmitCode)}.{nameof(SubmitCode.Parameters)}",

        $"{nameof(KernelCommand)}.{nameof(KernelCommand.TargetKernelName)}",
        $"{nameof(KernelCommand)}.{nameof(KernelCommand.DestinationUri)}",
        $"{nameof(KernelCommand)}.{nameof(KernelCommand.OriginUri)}",
    };

    private static readonly IEnumerable<Type> CoreAssemblyTypes = GetTypesFromClosure(typeof(KernelCommand).Assembly);
    private static readonly IEnumerable<Type> CSharpProjectKernelAssemblyTypes = GetTypesFromClosure(typeof(CSharpProjectKernel).Assembly);
    private static readonly IEnumerable<Type> VSCodeAssemblyTypes = typeof(VSCodeClientKernelExtension).Assembly.ExportedTypes;
    private static readonly IEnumerable<Type> AllAssemblyTypes = CoreAssemblyTypes.Concat(CSharpProjectKernelAssemblyTypes).Concat(VSCodeAssemblyTypes).Distinct();
            
    private static IEnumerable<Type> GetTypesFromClosure(Assembly rootAssembly)
    {
        return AssemblyToScan(rootAssembly).SelectMany(a => a.ExportedTypes).Distinct();

        static IEnumerable<Assembly> AssemblyToScan(Assembly rootAssembly)
        {
            var queue = new Queue<Assembly>();
            queue.Enqueue(rootAssembly);
            var scanned = new HashSet<string> { rootAssembly.GetName().FullName };
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var ran in current.GetReferencedAssemblies())
                {
                    if (scanned.Add(ran.FullName))
                    {
                        try
                        {
                            var referencedAssembly = Assembly.Load(ran);
                            queue.Enqueue(referencedAssembly);
                        }
                        catch (FileNotFoundException)
                        {
                                
                        }
                    }
                }

                yield return current;
            }
        }
    }

    public static string Generate()
    {
        var builder = new StringBuilder();

        var commandTypes = AllAssemblyTypes
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => typeof(KernelCommand).IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .ToList();
        var eventTypes = AllAssemblyTypes
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => typeof(KernelEvent).IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .ToList();

        var emittedTypes = new HashSet<Type>(WellKnownTypes.Keys);

        builder.AppendLine(@"// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Generated TypeScript interfaces and types.");

        var requiredTypes = new List<Type>();

        builder.AppendLine();
        builder.AppendLine("// --------------------------------------------- Kernel Commands");
        GenerateTypesAndInterfaces(builder, "KernelCommandType", commandTypes, emittedTypes, requiredTypes);

        builder.AppendLine();
        builder.AppendLine("// --------------------------------------------- Kernel events");
        GenerateTypesAndInterfaces(builder, "KernelEventType", eventTypes, emittedTypes, requiredTypes);
            
        builder.AppendLine();
        builder.AppendLine("// --------------------------------------------- Required Types");
        foreach (var type in requiredTypes.OrderBy(t => t.Name))
        {
            GenerateType(builder, type, emittedTypes, null);
        }
           
        builder.AppendLine();
        var staticContents = File.ReadAllText(Path.Combine(Path.GetDirectoryName(typeof(InterfaceGenerator).Assembly.Location), "StaticContents.ts"));
        builder.Append(staticContents);

        return builder.ToString();
    }

    private static void GenerateTypesAndInterfaces(StringBuilder builder, string collectiveTypeName,
        ICollection<Type> types, ISet<Type> emittedTypes, ICollection<Type> additionalTypes)
    {
        builder.AppendLine();
        foreach (var type in types)
        {
            builder.AppendLine($"export const {type.Name}Type = \"{type.Name}\";");
        }

        builder.AppendLine();
        builder.AppendLine($"export type {collectiveTypeName} =");
        builder.AppendLine($"      {string.Join($"{Environment.NewLine}    | ", types.Select(c => $"typeof {c.Name}Type"))};");

        foreach (var type in types)
        {
            GenerateType(builder, type, emittedTypes, additionalTypes);
        }

        foreach (var type in AlwaysEmitTypes)
        {
            GenerateType(builder, type, emittedTypes, additionalTypes);
        }

        foreach (var type in ParserServerTypes)
        {
            GenerateType(builder, type, emittedTypes, additionalTypes);
        }
    }

    private static void GenerateType(StringBuilder builder, Type type, ISet<Type> emittedTypes, ICollection<Type> requiredTypes)
    {
        if (!emittedTypes.Add(type))
        {
            // don't recreate
            return;
        }

        if (type.IsEnum)
        {
            GenerateEnum(builder, type);
            return;
        }

        var baseType = type.EffectiveBaseType();
        var extends = baseType is null
            ? ""
            : $"extends {GetTypeScriptTypeName(baseType)} ";

        builder.AppendLine();
        builder.AppendLine($"export interface {TypeName(type)} {extends}{{");

        foreach (var property in GetProperties(type).OrderBy(t => t.Name))
        {
            builder.AppendLine($"    {PropertyName(type, property)}: {GetTypeScriptTypeName(property.PropertyType)};");
        }

        builder.AppendLine("}");

        if (baseType is not null)
        {
            GenerateType(builder, baseType, emittedTypes, requiredTypes);
        }

        foreach (var propertyType in GetProperties(type).Select(p => GetUnderlyingType(p.PropertyType)).OrderBy(t => t.Name))
        {
            HandlePropertyType(propertyType);
            if (propertyType.IsAbstract)
            {
                foreach (var derivedPropertyType in AllAssemblyTypes.Where(t => t.IsSubclassOf(propertyType)))
                {
                    HandlePropertyType(derivedPropertyType);
                }
            }
        }

        void HandlePropertyType(Type propertyType)
        {
            if (requiredTypes is not null)
            {
                requiredTypes.Add(propertyType);
            }
            else
            {
                GenerateType(builder, propertyType, emittedTypes, null);
            }
        }
    }

    private static string TypeName(Type type)
    {
        if (WellKnownTypes.TryGetValue(type, out var wellKnownName))
        {
            return wellKnownName;
        }

        if (TypeNameOverrides.TryGetValue(type, out var overrideName))
        {
            return overrideName;
        }

        return type.Name;
    }

    private static string PropertyName(Type type, PropertyInfo propertyInfo)
    {
        var nullabilityContext = new NullabilityInfoContext().Create(propertyInfo);

        var isOptional = nullabilityContext.ReadState == NullabilityState.Nullable ||
                         OptionalFields.Contains($"{type.Name}.{propertyInfo.Name}");

        var propertyName = propertyInfo.Name.CamelCase();
        return isOptional
            ? propertyName + "?"
            : propertyName;
    }

    private static void GenerateEnum(StringBuilder builder, Type type)
    {
        builder.AppendLine();
        builder.AppendLine($"export enum {type.Name} {{");
        foreach (var name in type.GetEnumNames())
        {
            builder.AppendLine($"    {name} = \"{name.ToLowerInvariant()}\",");
        }

        builder.AppendLine("}");
    }

    private static string GetTypeScriptTypeName(Type type)
    {
        if (WellKnownTypes.TryGetValue(type, out var typeName))
        {
            return typeName;
        }

        if (type.ShouldBeDictionaryOfString())
        {
            return $"{{ [key: string]: {GetTypeScriptTypeName(type.GenericTypeArguments[1])}; }}";
        }

        if (type.ShouldBeArray())
        {
            return $"Array<{GetTypeScriptTypeName(type.GetArrayElementType())}>";
        }

        if (type.IsNullable())
        {
            return GetTypeScriptTypeName(type.GenericTypeArguments[0]);
        }

        return type.Name;
    }

    private static Type GetUnderlyingType(Type type)
    {
        if (type.ShouldBeDictionaryOfString())
        {
            return type.GenericTypeArguments[1];
        }

        if (type.ShouldBeArray())
        {
            return type.GetArrayElementType();
        }

        if (type.IsNullable())
        {
            return Nullable.GetUnderlyingType(type);
        }

        return type;
    }

    private static IEnumerable<PropertyInfo> GetProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.DeclaringType == type)
            .Where(p =>
            {
                var jsonIgnore = p.GetCustomAttribute(typeof(JsonIgnoreAttribute)) as JsonIgnoreAttribute;
                return jsonIgnore is null || jsonIgnore.Condition != JsonIgnoreCondition.Always;
            });
    }
}