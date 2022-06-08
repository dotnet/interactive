// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Mermaid;

[TypeFormatterSource(typeof(UmlClassDiagramFormatter))]
public class UmlClassDiagram
{
    private readonly Type _type;
    private UmlClassDiagramConfiguration? _classDiagramConfiguration;
    private readonly MermaidMarkdown _markDown;

    public UmlClassDiagram(Type type, UmlClassDiagramConfiguration? classDiagramConfiguration)
    {
        _type = type;
        _classDiagramConfiguration = classDiagramConfiguration;
        _markDown = ToClassDiagram(_type, _classDiagramConfiguration);
    }

    public MermaidMarkdown ToMarkdown()
    {
        return _markDown;
    }

    public UmlClassDiagram WithGraphDepth(int graphDepth)
    {
        _classDiagramConfiguration ??= new UmlClassDiagramConfiguration();
        _classDiagramConfiguration = _classDiagramConfiguration  with { GraphDepth = graphDepth };
        return this;
    }

    internal static MermaidMarkdown ToClassDiagram(Type type, UmlClassDiagramConfiguration? configuration = null)
    {

        var buffer = new StringBuilder();
        buffer.AppendLine("classDiagram");
        var types = new List<Type>();

        var classRelationshipBuffer = new StringBuilder();
        var classDefinitionBuffer = new StringBuilder();
        var generateTypes = new HashSet<Type>();

        CreateRelationships(type, configuration?.GraphDepth ?? 0, types, classRelationshipBuffer);

        foreach (var typeToDescribe in types.OrderBy(t => t.Name.ToLowerInvariant()))
        {

            if (generateTypes.Add(typeToDescribe))
            {
                var className = CreateClassName(typeToDescribe);

                classDefinitionBuffer.AppendLine($"class {className}");
                if (typeToDescribe.IsInterface)
                {
                    classDefinitionBuffer.AppendLine($"<<interface>> {className}");
                }

                foreach (var method in typeToDescribe.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public |
                                                                 BindingFlags.NonPublic | BindingFlags.Static |
                                                                 BindingFlags.Instance).OrderBy(m => m.Name.ToLowerInvariant()))
                {
                    classDefinitionBuffer.AppendLine($"{className} : {CreateMethodSignature(method)}");
                }

                classDefinitionBuffer.AppendLine();
            }
        }

        buffer.AppendLine(classRelationshipBuffer.ToString());

        buffer.AppendLine(classDefinitionBuffer.ToString());

        return new MermaidMarkdown(buffer.ToString());

        static string CreateClassName(Type classType, string openGeneric = "~", string closeGeneric = "~")
        {

            if (classType.IsGenericType)
            {
                var genericArgs = classType.GenericTypeArguments;
                var generic = classType.GetGenericTypeDefinition();
                var genericMarker = generic.Name.IndexOf("`");
                var name = genericMarker > 0 ? generic.Name.Substring(0, generic.Name.IndexOf("`")) : generic.Name;
                return $"{name}{openGeneric}{string.Join(", ", genericArgs.Select(t => CreateClassName(t, "<", ">")))}{closeGeneric}";
            }

            var marker = classType.Name.IndexOf("`");
            var safeName = marker > 0 ? classType.Name.Substring(0, classType.Name.IndexOf("`")) : classType.Name;

            return safeName;
        }

        static string CreateParameter(ParameterInfo parameterInfo)
        {
            return $"{CreateClassName(parameterInfo.ParameterType)} {parameterInfo.Name}";
        }

        static string CreateMethodSignature(MethodInfo method)
        {

            var signature = new StringBuilder();
            if (method.IsPublic)
            {
                signature.Append("+");
            }
            if (method.IsPrivate)
            {
                signature.Append("-");
            }

            signature.Append($"{method.Name}({string.Join(", ", method.GetParameters().Select(CreateParameter))})");

            signature.Append(" ");

            signature.Append(CreateClassName(method.ReturnType));

            if (method.IsAbstract)
            {
                signature.Append("*");
            }
            if (method.IsStatic)
            {
                signature.Append("$");
            }

            return signature.ToString();
        }

        static void CreateRelationships(Type type, int graphDepth, List<Type> types, StringBuilder classRelationshipBuffer)
        {
            types.Add(type);
            if (graphDepth > 0)
            {
                var typesToScan = new List<Type>();
                if (type.BaseType is not null)
                {
                    typesToScan.Add(type.BaseType);
                    types.Add(type.BaseType);
                }

                foreach (var @interface in type.GetInterfaces())
                {
                    typesToScan.Add(@interface);
                    types.Add(@interface);
                }

                foreach (var parentType in typesToScan.OrderBy(t => t.FullName))
                {
                    classRelationshipBuffer.AppendLine($"{CreateClassName(type)} --|> {CreateClassName(parentType)} : Inheritance");
                }

                foreach (var parentType in typesToScan.OrderBy(t => t.FullName))
                {
                    CreateRelationships(parentType, graphDepth - 1, types, classRelationshipBuffer);
                }
            }
        }
    }
}