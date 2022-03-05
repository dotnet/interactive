// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.CSharpProject.MLS.Project
{
    public static  class TextGenerator
    {
        private static readonly Random RandomGenerator = new Random();

        public static char GetLowerCaseLetter()
        {

            var num = RandomGenerator.Next(0, 26); // Zero to 25
            var let = (char)('a' + num);
            return @let;
        }
    }
}