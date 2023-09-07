// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Formatting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public class TypeFormatterSourceAttribute : Attribute
{
    public TypeFormatterSourceAttribute(Type formatterSourceType)
    {
        FormatterSourceType = formatterSourceType;
    }

    public Type FormatterSourceType { get; }

    public string[] PreferredMimeTypes { get; set; }
}