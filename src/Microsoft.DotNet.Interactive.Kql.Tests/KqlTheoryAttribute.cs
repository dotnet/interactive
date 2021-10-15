// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.DotNet.Interactive.Kql.Tests
{
    public sealed class KqlTheoryAttribute : TheoryAttribute
    {
        private static readonly string _skipReason;

        static KqlTheoryAttribute()
        {
            _skipReason = KqlFactAttribute.TestConnectionAndReturnSkipReason();
        }

        public KqlTheoryAttribute()
        {
            if (_skipReason is not null)
            {
                Skip = _skipReason;
            }
        }
    }
}
