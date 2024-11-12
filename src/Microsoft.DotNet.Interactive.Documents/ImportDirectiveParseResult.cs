// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Documents;

public class DirectiveParseResult
{
    public string? CommandName { get; set; }

    public IList<string> Errors { get; } = new List<string>();

    public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>();

    public IList<InputField> InputFields { get; } = new List<InputField>();
}