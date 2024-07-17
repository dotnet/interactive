// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;

namespace Microsoft.DotNet.Interactive.ExtensionLab;

public class ExplainCodeExtension
{
    public static Task LoadAsync(Kernel kernel)
    {
        var mermaidKernel = kernel
                            .FindKernels(k => k.KernelInfo.LanguageName == "Mermaid")
                            .FirstOrDefault();

        if (mermaidKernel is null)
        {
            throw new KernelException($"{nameof(ExplainCodeExtension)} requires a kernel that supports Mermaid language");
        }

        var kernelInfo = mermaidKernel.KernelInfo;

        kernel.VisitSubkernelsAndSelf(k =>
        {
            if (k is CSharpKernel csharpKernel)
            {
                var directive = new ExplainCSharpCodeDirective();

                csharpKernel.AddDirective(
                    directive,
                    async (_, context) =>
                    {
                        if (context.Command is SubmitCode command)
                        {
                            var source = command.Code.Replace(directive.Name, "");

                            var syntaxTree = CreateSyntaxTree(source);

                            var markdown = CreateInteractionDiagram(syntaxTree).Replace("\r\n", "\n");

                            await context.HandlingKernel.RootKernel.SendAsync(new SubmitCode(markdown,
                                                                                             targetKernelName: kernelInfo.LocalName));

                            context.Complete(command);
                        }
                    }
                );
                KernelInvocationContext.Current?.Display(
                    new HtmlString(@"<details><summary>ExplainCode</summary>
    <p>This extension generates Sequence diagrams from csharp code using Mermaid kernel.</p>
    </details>"),
                    "text/html");
            }
        });

        return Task.CompletedTask;
    }

    private static SyntaxTree CreateSyntaxTree(string source)
    {
        var options = new CSharpParseOptions(
                languageVersion: LanguageVersion.Preview,
                documentationMode: DocumentationMode.Parse,
                SourceCodeKind.Script)
            .WithFeatures(new[] { new KeyValuePair<string, string>("flow-analysis", "") });

        var syntaxTree = CSharpSyntaxTree.ParseText(source, options);
        return syntaxTree;
    }

    private static string CreateInteractionDiagram(SyntaxTree syntaxTree)
    {
        var md = new StringBuilder("sequenceDiagram");
        md.AppendLine();
        var actors = new Stack<string>();
        var methodsDeclarations = new HashSet<string>();

        actors.Push("CodeSubmission");

        var root = syntaxTree.GetCompilationUnitRoot();

        var declarations = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var declarationSyntax in declarations)
        {
            methodsDeclarations.Add(declarationSyntax.Identifier.ValueText);
        }

        foreach (var syntaxNode in root.ChildNodes())
        {
            WriteNode(syntaxNode, md, actors, methodsDeclarations, 1);
        }

        actors.Pop();
        return md.ToString();
    }

    private static void WriteNode(
        SyntaxNode syntaxNode,
        StringBuilder md,
        Stack<string> actors,
        HashSet<string> methodsDeclarations,
        int indentLevel)
    {
        var indent = new string(' ', indentLevel);
        switch (syntaxNode)
        {
            case BlockSyntax blockStatement:
                foreach (var blockChild in blockStatement.ChildNodes())
                {
                    WriteNode(blockChild, md, actors, methodsDeclarations, indentLevel);
                }

                break;

            case GlobalStatementSyntax globalStatement:
                WriteNode(globalStatement.Statement, md, actors, methodsDeclarations, indentLevel);
                break;

            case ForStatementSyntax forStatement:
                md.AppendLine($"{indent}loop");
                foreach (var forChild in forStatement.ChildNodes())
                {
                    WriteNode(forChild, md, actors, methodsDeclarations, indentLevel++);
                }

                md.AppendLine($"{indent}end");
                break;

            case ExpressionStatementSyntax expressionStatement:
                foreach (var expressionChild in expressionStatement.ChildNodes())
                {
                    WriteNode(expressionChild, md, actors, methodsDeclarations, indentLevel);
                }

                break;

            case InvocationExpressionSyntax invocationExpression:
                var source = actors.Peek();
                var target = "";
                var method = "";
                foreach (var invocationChild in invocationExpression.ChildNodes())
                {
                    switch (invocationChild)
                    {
                        case MemberAccessExpressionSyntax memberAccessExpression:
                            target = memberAccessExpression.Expression.ToString();
                            method = memberAccessExpression.Name.ToString();
                            break;
                        case ArgumentListSyntax argumentList:
                            foreach (var argument in argumentList.Arguments)
                            {
                                WriteNode(argument.Expression, md, actors, methodsDeclarations, indentLevel);
                            }

                            break;

                        default:
                            // skip other nodes
                            break;
                    }
                }

                md.AppendLine($"{indent}{source}->>+{target}: invoke {method}");
                md.AppendLine($"{indent}{target}->>-{source}: return");
                break;

            default:
                // skip other nodes
                break;
        }
    }
}