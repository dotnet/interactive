// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.DotNet.Interactive.CSharp
{
    public static class LanguageServiceExtensions
    {
        public static string ToMarkdownString(this QuickInfoItem info)
        {
            var stringBuilder = new StringBuilder();
            var description = info.Sections.FirstOrDefault(s => QuickInfoSectionKinds.Description.Equals(s.Kind))?.Text ?? string.Empty;
            var documentation = info.Sections.FirstOrDefault(s => QuickInfoSectionKinds.DocumentationComments.Equals(s.Kind))?.Text ?? string.Empty;

            if (!string.IsNullOrEmpty(description))
            {
                stringBuilder.Append(description);
                if (!string.IsNullOrEmpty(documentation))
                {
                    stringBuilder.Append("\r\n> ").Append(documentation);
                }
            }

            return stringBuilder.ToString();
        }
    }
}
