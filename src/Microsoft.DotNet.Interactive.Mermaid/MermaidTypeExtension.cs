// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Mermaid;

// ReSharper disable once CheckNamespace
namespace System;

public static class MermaidTypeExtension
{
    public static UmlClassDiagram ToUmlClassDiagram(this Type type, UmlClassDiagramConfiguration? configuration = null)
    {
        return new UmlClassDiagram(type, configuration ?? new UmlClassDiagramConfiguration(0));
    }
}