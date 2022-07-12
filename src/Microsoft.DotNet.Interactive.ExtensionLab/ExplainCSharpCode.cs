// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.ExtensionLab;

public class ExplainCSharpCode : Command
{
    public ExplainCSharpCode(KernelInfo mermaidKernelInfo) : base("#!explain",
        "Explain csharp code with Sequence diagrams.")
    {
        var kernelInfo = mermaidKernelInfo ?? throw new ArgumentNullException(nameof(mermaidKernelInfo));
        Handler = CommandHandler.Create(async (KernelInvocationContext context) =>
        {
            if (context.Command is SubmitCode command)
            {
                var source = command.Code.Replace(Name, "");

                var syntaxTree = CreateSyntaxTree(source);

                var markdown = CreateInteractionDiagram(syntaxTree).Replace("\r\n", "\n");

                await context.HandlingKernel.RootKernel.SendAsync(new SubmitCode(markdown,
                    targetKernelName: kernelInfo.LocalName));

                context.Complete(command);
            }
        });
    }

    private static SyntaxTree CreateSyntaxTree(string source)
    {
        var options = new CSharpParseOptions(
                languageVersion: LanguageVersion.Preview,
                documentationMode: DocumentationMode.Parse,
                SourceCodeKind.Script)
            .WithFeatures(new[] {new KeyValuePair<string, string>("flow-analysis", "")});

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

    private static void WriteNode(SyntaxNode syntaxNode, StringBuilder md, Stack<string> actors, HashSet<string> methodsDeclarations, int indentLevel)
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