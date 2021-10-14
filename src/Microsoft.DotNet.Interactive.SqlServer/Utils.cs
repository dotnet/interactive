using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    internal static class Utils
    {
        public static string AsSingleQuotedString(this string str)
        {
            return $"'{str.Replace("'", "''")}'";
        }
    }
}
