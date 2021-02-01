// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.DotNet.Interactive.CSharp.SignatureHelp
{
    // Remove this class once a public API for Signature Help from Roslyn is available
    internal static class CheckForStaticExtension
    {
        public static bool IsInStaticContext(this SyntaxNode node)
        {
            // this/base calls are always static.
            if (node.FirstAncestorOrSelf<ConstructorInitializerSyntax>() != null)
            {
                return true;
            }

            var memberDeclaration = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            if (memberDeclaration == null)
            {
                return false;
            }

            switch (memberDeclaration.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.IndexerDeclaration:
                    return GetModifiers(memberDeclaration).Any(SyntaxKind.StaticKeyword);

                case SyntaxKind.PropertyDeclaration:
                    return GetModifiers(memberDeclaration).Any(SyntaxKind.StaticKeyword) ||
                        node.IsFoundUnder((PropertyDeclarationSyntax p) => p.Initializer);

                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                    // Inside a field one can only access static members of a type (unless it's top-level).
                    return !memberDeclaration.Parent.IsKind(SyntaxKind.CompilationUnit);

                case SyntaxKind.DestructorDeclaration:
                    return false;
            }

            // Global statements are not a static context.
            if (node.FirstAncestorOrSelf<GlobalStatementSyntax>() != null)
            {
                return false;
            }

            // any other location is considered static
            return true;
        }

        public static SyntaxTokenList GetModifiers(SyntaxNode member)
        {
            if (member != null)
            {
                switch (member.Kind())
                {
                    case SyntaxKind.EnumDeclaration:
                        return ((EnumDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.StructDeclaration:
                        return ((TypeDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.FieldDeclaration:
                        return ((FieldDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.EventFieldDeclaration:
                        return ((EventFieldDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.ConstructorDeclaration:
                        return ((ConstructorDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.DestructorDeclaration:
                        return ((DestructorDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.PropertyDeclaration:
                        return ((PropertyDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.EventDeclaration:
                        return ((EventDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.IndexerDeclaration:
                        return ((IndexerDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.OperatorDeclaration:
                        return ((OperatorDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.ConversionOperatorDeclaration:
                        return ((ConversionOperatorDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                        return ((AccessorDeclarationSyntax)member).Modifiers;
                }
            }

            return default;
        }

        public static bool IsFoundUnder<TParent>(this SyntaxNode node, Func<TParent, SyntaxNode> childGetter)
           where TParent : SyntaxNode
        {
            var ancestor = node.GetAncestor<TParent>();
            if (ancestor == null)
            {
                return false;
            }

            var child = childGetter(ancestor);

            // See if node passes through child on the way up to ancestor.
            return node.GetAncestorsOrThis<SyntaxNode>().Contains(child);
        }

        public static TNode GetAncestor<TNode>(this SyntaxNode node)
           where TNode : SyntaxNode
        {
            var current = node.Parent;
            while (current != null)
            {
                if (current is TNode tNode)
                {
                    return tNode;
                }

                current = current.GetParent();
            }

            return null;
        }

        private static SyntaxNode GetParent(this SyntaxNode node)
        {
            return node is IStructuredTriviaSyntax trivia ? trivia.ParentTrivia.Token.Parent : node.Parent;
        }

        public static IEnumerable<TNode> GetAncestorsOrThis<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            var current = node;
            while (current != null)
            {
                if (current is TNode tNode)
                {
                    yield return tNode;
                }

                current = current.GetParent();
            }
        }
    }
}
