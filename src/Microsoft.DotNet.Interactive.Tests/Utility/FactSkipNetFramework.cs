// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public sealed class NotNetFrameworkConditionAttribute : ConditionBaseAttribute
{
    private readonly string _reason;

    public NotNetFrameworkConditionAttribute(string reason = null)
        : base(ConditionMode.Include)
    {
        _reason = string.IsNullOrWhiteSpace(reason) ? "Ignored on .NET Framework" : reason;
    }

    public override string IgnoreMessage => _reason;

    public override string GroupName => nameof(NotNetFrameworkConditionAttribute);

    public override bool ShouldRun => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
}
