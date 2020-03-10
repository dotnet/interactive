// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.PowerShell.Host
{
    internal static class VTColorUtils
    {
        internal const string EscapeCharacters = "\x1b[";
        internal const string ResetColor = "\x1b[0m";
        internal const string BoldColor = "\x1b[1m";
        internal const ConsoleColor DefaultConsoleColor = (ConsoleColor)(-1);

        private static readonly string[] BackgroundColorMap = {
            "\x1b[40m", // Black
            "\x1b[44m", // DarkBlue
            "\x1b[42m", // DarkGreen
            "\x1b[46m", // DarkCyan
            "\x1b[41m", // DarkRed
            "\x1b[45m", // DarkMagenta
            "\x1b[43m", // DarkYellow
            "\x1b[47m", // Gray
            "\x1b[100m", // DarkGray
            "\x1b[104m", // Blue
            "\x1b[102m", // Green
            "\x1b[106m", // Cyan
            "\x1b[101m", // Red
            "\x1b[105m", // Magenta
            "\x1b[103m", // Yellow
            "\x1b[107m", // White
        };

        private static readonly string[] ForegroundColorMap = {
            "\x1b[30m", // Black
            "\x1b[34m", // DarkBlue
            "\x1b[32m", // DarkGreen
            "\x1b[36m", // DarkCyan
            "\x1b[31m", // DarkRed
            "\x1b[35m", // DarkMagenta
            "\x1b[33m", // DarkYellow
            "\x1b[37m", // Gray
            "\x1b[90m", // DarkGray
            "\x1b[94m", // Blue
            "\x1b[92m", // Green
            "\x1b[96m", // Cyan
            "\x1b[91m", // Red
            "\x1b[95m", // Magenta
            "\x1b[93m", // Yellow
            "\x1b[97m", // White
        };

        private static bool IsValidColor(ConsoleColor color, out bool isDefaultColor)
        {
            isDefaultColor = color == DefaultConsoleColor;
            return isDefaultColor ||
                   (color >= 0 && color < (ConsoleColor) ForegroundColorMap.Length);
        }

        internal static string CombineColorSequences(ConsoleColor fg, ConsoleColor bg)
        {
            if (!IsValidColor(fg, out bool fgIsDefaultColor))
            {
                throw new ArgumentOutOfRangeException(nameof(fg));
            }

            if (!IsValidColor(bg, out bool bgIsDefaultColor))
            {
                throw new ArgumentOutOfRangeException(nameof(bg));
            }

            if (fgIsDefaultColor && bgIsDefaultColor)
            {
                return string.Empty;
            }
            else if (fgIsDefaultColor)
            {
                return MapColorToEscapeSequence(bg, isBackground: true);
            }
            else if (bgIsDefaultColor)
            {
                return MapColorToEscapeSequence(fg, isBackground: false);
            }

            // Both foreground and background colors are not the default.
            return CombineColorSequences(ForegroundColorMap[(int)fg], BackgroundColorMap[(int)bg]);
        }

        internal static string CombineColorSequences(string fg, string bg)
        {
            string ExtractCode(string s)
            {
                return s.Substring(2).TrimEnd(new[] {'m'});
            }

            return "\x1b[" + ExtractCode(fg) + ";" + ExtractCode(bg) + "m";
        }

        internal static string MapColorToEscapeSequence(ConsoleColor color, bool isBackground)
        {
            int index = (int) color;
            if (index < 0)
            {
                // TODO: light vs. dark
                if (isBackground)
                {
                    // Don't change the background - the default (unknown) background
                    // might be subtly or completely different than what we choose and
                    // look weird.
                    return string.Empty;
                }

                return ForegroundColorMap[(int) ConsoleColor.Gray];
            }

            if (index > ForegroundColorMap.Length)
            {
                return string.Empty;
            }

            return (isBackground ? BackgroundColorMap : ForegroundColorMap)[index];
        }
    }
}
