// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Tests
{
    public static class LanguageExtensions
    {
        public static string LanguageName(this Language language)
        {
            return language switch
            {
                Language.CSharp => "csharp",
                Language.FSharp => "fsharp",
                Language.PowerShell => "pwsh",
                _ => null
            };
        }
    }
}