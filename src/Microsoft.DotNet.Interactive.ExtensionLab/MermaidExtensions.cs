// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Http;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{

    public record ClassDiagramConfiguration(int GraphDepth);

    public class UmlClassDiagramExplorer
    {
        private readonly Type _type;
        private ClassDiagramConfiguration _classDiagramConfiguration;

        public UmlClassDiagramExplorer(Type type, ClassDiagramConfiguration classDiagramConfiguration)
        {
            _type = type;
            _classDiagramConfiguration = classDiagramConfiguration;
        }

        public MermaidMarkdown ToMarkdown()
        {
            return _type.ToClassDiagram(_classDiagramConfiguration);
        }

        public UmlClassDiagramExplorer WithGraphDepth(int graphDepth)
        {
            _classDiagramConfiguration = _classDiagramConfiguration with { GraphDepth = graphDepth };
            return this;
        }
    }

    public static class MermaidExtensions
    {
        public static MermaidMarkdown ToClassDiagram(this Type type, ClassDiagramConfiguration configuration = null)
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

                signature.Append($"{method.Name}({ string.Join(", ", method.GetParameters().Select(CreateParameter))})");

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

        public static T UseMermaid<T>(this T kernel, Uri libraryUri = null, string libraryVersion = null, string cacheBuster = null) where T : Kernel
        {
            cacheBuster = string.IsNullOrWhiteSpace(cacheBuster) ? Guid.NewGuid().ToString("N") : cacheBuster;
            Formatter.Register<MermaidMarkdown>((markdown, writer) =>
            {
                var html = GenerateHtml(markdown, libraryUri,
                    libraryVersion,
                    cacheBuster);
                html.WriteTo(writer, HtmlEncoder.Default);
            }, HtmlFormatter.MimeType);

            Formatter.Register<UmlClassDiagramExplorer>((explorer, writer) =>
            {
                var markDown = explorer.ToMarkdown();
                writer.Write(markDown.ToDisplayString(HtmlFormatter.MimeType));
            }, HtmlFormatter.MimeType);

            return kernel;
        }

        private static IHtmlContent GenerateHtml(MermaidMarkdown markdown, Uri libraryUri, string libraryVersion, string cacheBuster)
        {
            var requireUri = new Uri("https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js");
            var divId = Guid.NewGuid().ToString("N");
            var code = new StringBuilder();
            var functionName = $"loadMermaid_{divId}";
            code.AppendLine("<div style=\"background-color:white;\">");

            code.AppendLine(@"<script type=""text/javascript"">");
            code.AppendJsCode(divId, functionName, libraryUri, libraryVersion, cacheBuster, markdown.ToString());
            code.AppendLine(JavascriptUtilities.GetCodeForEnsureRequireJs(requireUri, functionName));
            code.AppendLine("</script>");

            code.AppendLine($"<div id=\"{divId}\"></div>");
            code.AppendLine("</div>");

            var html = new HtmlString(code.ToString());
            return html;
        }

        private static void AppendJsCode(this StringBuilder stringBuilder,
            string divId, string functionName, Uri libraryUri, string libraryVersion, string cacheBuster, string markdown)
        {
            libraryVersion ??= "1.0.0";
            stringBuilder.AppendLine($@"
let {functionName} = () => {{");
            if (libraryUri is not null)
            {
                var libraryAbsoluteUri = libraryUri.AbsoluteUri.Replace(".js", string.Empty);
                cacheBuster ??= Guid.NewGuid().ToString("N");
                stringBuilder.AppendLine($@" 
        (require.config({{ 'paths': {{ 'context': '{libraryVersion}', 'mermaidUri' : '{libraryAbsoluteUri}', 'urlArgs': 'cacheBuster={cacheBuster}' }}}}) || require)(['mermaidUri'], (mermaid) => {{");
            }
            else
            {
                stringBuilder.AppendLine($@"
        configureRequireFromExtension('Mermaid','{libraryVersion}')(['Mermaid/mermaidapi'], (mermaid) => {{");
            }

            stringBuilder.AppendLine($@"
            let renderTarget = document.getElementById('{divId}');
            mermaid.render( 
                'mermaid_{divId}', 
                `{markdown}`, 
                g => {{
                    renderTarget.innerHTML = g 
                }});
        }},
        (error) => {{
            console.log(error);
        }});
}}");
        }
    }
}