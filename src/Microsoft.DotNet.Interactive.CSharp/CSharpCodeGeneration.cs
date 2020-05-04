// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.CSharp
{
    internal static partial class TypeExtensions
    {
        public static void WriteCSharpDeclarationTo(
            this Type type,
            TextWriter writer)
        {
            var typeName = type.FullName ?? type.Name;

            if (typeName.Contains("`"))
            {
                writer.Write(typeName.Remove(typeName.IndexOf('`')));
                writer.Write("<");
                var genericArguments = type.GetGenericArguments();

                for (var i = 0; i < genericArguments.Length; i++)
                {
                    genericArguments[i].WriteCSharpDeclarationTo(writer);
                    if (i < genericArguments.Length - 1)
                    {
                        writer.Write(",");
                    }
                }

                writer.Write(">");
            }
            else
            {
                writer.Write(typeName);
            }
        }
    }
}