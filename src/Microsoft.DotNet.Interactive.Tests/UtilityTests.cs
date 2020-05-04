// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class UtilityTests
    {
        [Fact(Timeout = 45000)]
        public void replacement_position_can_be_found_at_the_end_of_a_string()
        {
            var code = "System.Linq.Enumerable.";
            // finding this                   ^
            var pos = SourceUtilities.ComputeReplacementStartPosition(code, code.Length);
            Assert.Equal(code.Length, pos);
        }

        [Fact(Timeout = 45000)]
        public void replacement_position_can_be_found_not_at_the_end_of_a_string()
        {
            var code = "System.Linq.Enumerable.Ran";
            // finding this                   ^
            var lastDotPos = code.LastIndexOf('.') + 1;
            var pos = SourceUtilities.ComputeReplacementStartPosition(code, lastDotPos);
            Assert.Equal(lastDotPos, pos);
        }

        [Fact(Timeout = 45000)]
        public void replacement_position_can_be_found_at_the_end_of_a_multiline_string()
        {
            var code = @"
using System.Linq;
Enumerable.
//        ^ finding this";
            var lastDotPos = code.LastIndexOf('.') + 1;
            var pos = SourceUtilities.ComputeReplacementStartPosition(code, lastDotPos);
            Assert.Equal(lastDotPos, pos);
        }

        [Fact(Timeout = 45000)]
        public void replacement_position_can_be_found_not_at_the_end_of_a_multiline_string()
        {
            var code = @"
using System.Linq;
Enumerable.Ran
//        ^ finding this";
            var lastDotPos = code.LastIndexOf('.') + 1;
            var pos = SourceUtilities.ComputeReplacementStartPosition(code, lastDotPos);
            Assert.Equal(lastDotPos, pos);
        }
    }
}
