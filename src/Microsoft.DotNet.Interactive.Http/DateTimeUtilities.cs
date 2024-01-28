using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Http
{

    internal static class DateTimeUtilities
    {
        public static DateTimeOffset AddOffset(this DateTimeOffset dateTime, int offset, string offsetFormatString)
        {
            return offsetFormatString switch
            {
                "ms" => dateTime.AddMilliseconds(offset),
                "s" => dateTime.AddSeconds(offset),
                "m" => dateTime.AddMinutes(offset),
                "h" => dateTime.AddHours(offset),
                "d" => dateTime.AddDays(offset),
                "w" => dateTime.AddDays(offset * 7),
                "M" => dateTime.AddMonths(offset),
                "Q" => dateTime.AddMonths(offset * 3),
                "y" => dateTime.AddYears(offset),
                _ => throw new ArgumentException("Invalid offset format string", nameof(offsetFormatString))
            };
        }
    }
}
