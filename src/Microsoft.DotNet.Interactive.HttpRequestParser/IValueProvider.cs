// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Interactive.HttpRequest
{
    internal interface IValueProvider
    {
        public int Priority { get; }
        public string? Prefix { get; }
        public string ParseValues(string text, IReadOnlyDictionary<string, ParsedVariable> variablesExpanded, IHttpDocument document);
        public Regex? Pattern { get; }
    }
}