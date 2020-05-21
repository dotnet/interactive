// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.InterfaceGen.App
{
    public class InterfaceGenerator
    {
        private static readonly Dictionary<Type, string> WellKnownTypes = new Dictionary<Type, string>()
        {
            { typeof(bool), "boolean" },
            { typeof(int), "number" },
            { typeof(object), "any" },
            { typeof(string), "string" },

            { typeof(KernelCommandBase), "KernelCommand" },
            { typeof(KernelEventBase), "KernelEvent" },

            { typeof(DirectoryInfo), "string" },
            { typeof(FileInfo), "string" },
            { typeof(IKernelCommand), "KernelCommand" },
        };

        private static readonly HashSet<Type> AlwaysEmitTypes = new HashSet<Type>()
        {
            typeof(KernelCommandBase),
            typeof(KernelEventBase)
        };

        private static readonly HashSet<string> OptionalFields = new HashSet<string>()
        {
            $"{nameof(KernelCommandBase)}.{nameof(KernelCommandBase.TargetKernelName)}",
            $"{nameof(SubmitCode)}.{nameof(SubmitCode.SubmissionType)}"
        };

        public static string Generate()
        {
            var builder = new StringBuilder();
            var commandTypes = typeof(IKernelCommand)
                               .Assembly
                               .ExportedTypes
                               .Where(t => !t.IsAbstract && !t.IsInterface)
                               .Where(t => typeof(IKernelCommand).IsAssignableFrom(t))
                               .OrderBy(t => t.Name);
            var eventTypes = typeof(IKernelEvent)
                             .Assembly
                             .ExportedTypes
                             .Where(t => !t.IsAbstract && !t.IsInterface)
                             .Where(t => typeof(IKernelEvent).IsAssignableFrom(t))
                             .OrderBy(t => t.Name);

            var emittedTypes = new HashSet<Type>(WellKnownTypes.Keys);

            emittedTypes.RemoveWhere(AlwaysEmitTypes.Contains);

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
            IEnumerable<Type> types, HashSet<Type> emittedTypes, List<Type> additionalTypes)
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
        }

        private static void GenerateType(StringBuilder builder, Type type, HashSet<Type> emittedTypes,
            ICollection<Type> additionalTypes)
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
            var extends = baseType == null
                ? ""
                : $"extends {GetTypeScriptTypeName(baseType)} ";

            builder.AppendLine();
            builder.AppendLine($"export interface {TypeName(type)} {extends}{{");
            foreach (var property in GetProperties(type))
            {
                builder.AppendLine($"    {PropertyName(type, property)}: {GetTypeScriptTypeName(property.PropertyType)};");
            }

            builder.AppendLine("}");

            if (baseType != null)
            {
                GenerateType(builder, baseType, emittedTypes, additionalTypes);
            }

            foreach (var propertyType in GetProperties(type).Select(p => GetUnderlyingType(p.PropertyType)).OrderBy(t => t.Name))
            {
                if (additionalTypes != null)
                {
                    additionalTypes.Add(propertyType);
                }
                else
                {
                    GenerateType(builder, propertyType, emittedTypes, null);
                }
            }
        }

        private static string TypeName(Type type)
        {
            if (WellKnownTypes.TryGetValue(type, out var name))
            {
                return name;
            }

            return type.Name;
        }

        private static string PropertyName(Type type, PropertyInfo propertyInfo)
        {
            var isOptional = OptionalFields.Contains($"{type.Name}.{propertyInfo.Name}") 
                             || propertyInfo.PropertyType.IsNullable();

            var propertyName = propertyInfo.Name.CamelCase();
            return isOptional
                ? propertyName + "?"
                : propertyName;
        }

        private static void GenerateEnum(StringBuilder builder, Type type)
        {
            builder.AppendLine();
            builder.AppendLine($"export enum {type.Name} {{");
            foreach (var (name, value) in type.GetEnumNames().Zip(type.GetEnumValues().Cast<int>()))
            {
                builder.AppendLine($"    {name} = {value},");
            }

            builder.AppendLine("}");
        }

        private static string GetTypeScriptTypeName(Type type)
        {
            if (WellKnownTypes.TryGetValue(type, out var typeName))
            {
                return typeName;
            }

            if (type.ShouldBeArray())
            {
                return $"Array<{GetTypeScriptTypeName(type.GenericTypeArguments[0])}>";
            }

            if (type.IsNullable())
            {
                return GetTypeScriptTypeName(type.GenericTypeArguments[0]);
            }

            return type.Name;
        }

        private static Type GetUnderlyingType(Type type)
        {
            if (type.ShouldBeArray())
            {
                return type.GenericTypeArguments[0];
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
                .Where(p => p.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null);
        }
    }
}
