// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// adapted from: https://github.com/dotnet/roslyn/blob/master/src/VisualStudio/Core/Def/Implementation/ProjectSystem/AbstractEntryPointFinder.cs

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.CSharpProject
{
    public abstract class AbstractEntryPointFinder : SymbolVisitor
    {
#pragma warning disable RS1024 // Compare symbols correctly
        protected readonly HashSet<IMethodSymbol> EntryPoints = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            foreach (var member in symbol.GetMembers())
            {
                member.Accept(this);
            }
        }
 
        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            foreach (var member in symbol.GetMembers())
            {
                member.Accept(this);
            }
        }
 
        public override void VisitMethod(IMethodSymbol symbol)
        {
            // named Main
            if (!MatchesMainMethodName(symbol.Name))
            {
                return;
            }
 
            // static
            if (!symbol.IsStatic)
            {
                return;
            }
 
            // returns void or int
            if (!symbol.ReturnsVoid && symbol.ReturnType.SpecialType != SpecialType.System_Int32)
            {
                return;
            }
 
            // parameterless or takes a string[]
            if (symbol.Parameters.Length == 1)
            {
                var parameter = symbol.Parameters.Single();
                if (parameter.Type is IArrayTypeSymbol typeSymbol)
                {
                    var elementType = typeSymbol.ElementType;
                    var specialType = elementType.SpecialType;
 
                    if (specialType == SpecialType.System_String)
                    {
                        EntryPoints.Add(symbol);
                    }
                }
            }
 
            if (!symbol.Parameters.Any())
            {
                EntryPoints.Add(symbol);
            }
        }
 
        protected abstract bool MatchesMainMethodName(string name);
    }
}