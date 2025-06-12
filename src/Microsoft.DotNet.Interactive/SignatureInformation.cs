// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

public class SignatureInformation
{
    public SignatureInformation(string label, FormattedValue documentation, IReadOnlyList<ParameterInformation> parameters)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Documentation = documentation ?? throw new ArgumentNullException(nameof(documentation));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    public string Label { get; }
    public FormattedValue Documentation { get; }
    public IReadOnlyList<ParameterInformation> Parameters { get; }
}