// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable 8509
namespace Microsoft.DotNet.Interactive.Tests
{
    public enum Language
    {
        CSharp = 0,
        FSharp = 1,
        PowerShell = 2,
    }

    public static class LanguageExtensions
    {
        public static string LanguageName(this Language language)
        {
            return language switch
            {
                Language.CSharp => "csharp",
                Language.FSharp => "fsharp",
                Language.PowerShell => "pwsh",
            };
        }
    }
}
