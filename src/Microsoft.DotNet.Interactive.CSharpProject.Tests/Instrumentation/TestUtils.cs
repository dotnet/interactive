// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests.Instrumentation;

public static class TestUtils
{
    public static string RemoveWhitespace(string input) => Regex.Replace(input, @"\s", "");
}