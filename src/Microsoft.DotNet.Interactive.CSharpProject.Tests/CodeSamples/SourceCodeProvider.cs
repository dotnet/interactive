// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests.CodeSamples;

internal static class SourceCodeProvider
{
    public static string ConsoleProgramCollidingRegions =>
        @"using System;

namespace ConsoleProgramSingleRegion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #region alpha
            var a = 10;
            #endregion

            #region alpha
            var b = 10;
            #endregion
        }
    }
}".EnforceLF();

    public static string ConsoleProgramSingleRegion =>
        @"using System;

namespace ConsoleProgramSingleRegion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #region alpha
            var a = 10;
            #endregion
        }
    }
}".EnforceLF();


    public static string ConsoleProgramSingleRegionExtraUsing =>
        @"using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleProgramSingleRegion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #region alpha
            var a = 10;
            #endregion
        }
    }
}".EnforceLF();
}