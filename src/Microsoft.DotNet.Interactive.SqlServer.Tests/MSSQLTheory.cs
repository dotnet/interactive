// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.DotNet.Interactive.SqlServer.Tests
{
    public sealed class MsSqlTheory : TheoryAttribute
    {
        private static readonly string _skipReason;

        static MsSqlTheory()
        {
            _skipReason = MsSqlFact.TestConnectionAndReturnSkipReason();
        }

        public MsSqlTheory()
        {
            if (_skipReason is not null)
            {
                Skip = _skipReason;
            }
        }
    }
}