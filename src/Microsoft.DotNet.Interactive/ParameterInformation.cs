// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

public class ParameterInformation
{
    public ParameterInformation(string label, FormattedValue documentation)
    {
        Label = label;
        Documentation = documentation;
    }

    public string Label { get; }

    public FormattedValue Documentation { get; }
}