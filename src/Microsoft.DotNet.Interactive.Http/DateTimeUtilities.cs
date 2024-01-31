using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.Http
{

    internal static class DateTimeUtilities
    {
        public static bool TryAddOffset(this DateTimeOffset dateTime, int offset, string offsetFormatString, [NotNullWhen(true)] out DateTimeOffset? newDateTime)
        {
            newDateTime = offsetFormatString switch
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
                _ => null
            };

            return newDateTime != null;
        }
    }
}
