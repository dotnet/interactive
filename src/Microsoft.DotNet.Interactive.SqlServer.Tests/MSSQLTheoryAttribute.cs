// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.DotNet.Interactive.SqlServer.Tests;

public sealed class MsSqlTheoryAttribute : ConditionBaseAttribute
{
    private static readonly string _skipReason;

    static MsSqlTheoryAttribute()
    {
        _skipReason = MsSqlFactAttribute.TestConnectionAndReturnSkipReason();
    }

    public MsSqlTheoryAttribute()
        : base(ConditionMode.Include)
    {
    }

    public override string IgnoreMessage => _skipReason;

    public override string GroupName => nameof(MsSqlTheoryAttribute);

    public override bool ShouldRun => _skipReason is null;
}