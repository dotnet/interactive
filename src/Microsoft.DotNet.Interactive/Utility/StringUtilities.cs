using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.Interactive.Utility
{
    public static class StringUtilities
    {
        public static string NormalizeLineEndings(this string source)
        {
            return source.Replace("\r\n", "\n");
        }
    }
}
