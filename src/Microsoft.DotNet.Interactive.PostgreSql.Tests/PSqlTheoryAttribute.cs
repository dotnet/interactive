// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.DotNet.Interactive.PostgreSql.Tests;

public sealed class PSqlTheoryAttribute : TheoryAttribute
{
    private static readonly string _skipReason;

    static PSqlTheoryAttribute()
    {
        _skipReason = PSqlFactAttribute.TestConnectionAndReturnSkipReason();
    }

    public PSqlTheoryAttribute()
    {
        if (_skipReason is not null)
        {
            Skip = _skipReason;
        }
    }
}