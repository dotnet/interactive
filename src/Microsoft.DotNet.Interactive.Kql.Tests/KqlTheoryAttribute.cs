// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.DotNet.Interactive.Kql.Tests;

public sealed class KqlTheoryAttribute : ConditionBaseAttribute
{
    private static readonly string _skipReason;

    static KqlTheoryAttribute()
    {
        _skipReason = KqlFactAttribute.TestConnectionAndReturnSkipReason();
    }

    public KqlTheoryAttribute()
        : base(ConditionMode.Include)
    {
    }

    public override string IgnoreMessage => _skipReason;

    public override string GroupName => nameof(KqlTheoryAttribute);

    public override bool ShouldRun => _skipReason is null;
}