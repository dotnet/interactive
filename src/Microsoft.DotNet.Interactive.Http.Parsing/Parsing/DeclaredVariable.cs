// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Http.Parsing.Parsing;

internal class DeclaredVariable
{
    public string Name { get; }
    public string Value { get; }

    public HttpBindingResult<string> HttpBindingResult { get; }

    public DeclaredVariable(string name, string value, HttpBindingResult<string> httpBindingResult)
    {
        Name = name;
        Value = value;
        HttpBindingResult = httpBindingResult;
    }
}