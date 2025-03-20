// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.DotNet.Interactive.PostgreSql.Tests;

public sealed class PostgreSqlTheoryAttribute : ConditionBaseAttribute
{
    private static readonly string _skipReason;

    static PostgreSqlTheoryAttribute()
    {
        _skipReason = PostgreSqlFactAttribute.TestConnectionAndReturnSkipReason();
    }

    public PostgreSqlTheoryAttribute()
        : base(ConditionMode.Include)
    {
    }

    public override string IgnoreMessage => _skipReason;

    public override string GroupName => nameof(PostgreSqlTheoryAttribute);

    public override bool ShouldRun => _skipReason is null;
}