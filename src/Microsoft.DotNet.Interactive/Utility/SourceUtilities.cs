// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Utility
{
    public static class SourceUtilities
    {
        private static readonly Regex _lastToken = new Regex(@"(?<lastToken>\S+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Given the specified code and cursor position, finds the location where replacement text will begin
        /// insertion, usually the location of the last dot ('.').
        /// </summary>
        public static int ComputeReplacementStartPosition(string code, int cursorPosition)
        {
            var pos = cursorPosition;

            if (pos > 0)
            {
                var codeToCursor = code.Substring(0, pos);
                var match = _lastToken.Match(codeToCursor);
                if (match.Success)
                {
                    var token = match.Groups["lastToken"];
                    if (token.Success)
                    {
                        var lastDotPosition = token.Value.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase);
                        if (lastDotPosition >= 0)
                        {
                            pos = token.Index + lastDotPosition + 1;
                        }
                        else
                        {
                            pos = token.Index;
                        }
                    }
                }
            }

            return pos;
        }

        /// <summary>
        /// Computes the absolute offset corresponding to the given position.
        /// </summary>
        public static int GetCursorOffsetFromPosition(string code, LinePosition position)
        {
            int line = 0;
            int character = 0;
            int offset = 0;
            for (; offset < code.Length; offset++)
            {
                if (line >= position.Line && character >= position.Character)
                {
                    break;
                }

                switch (code[offset])
                {
                    case '\n':
                        line++;
                        character = 0;
                        break;
                    default:
                        character++;
                        break;
                }
            }

            return offset;
        }

        /// <summary>
        /// Computes the line/character position based on the given cursor offset.
        /// </summary>
        public static LinePosition GetPositionFromCursorOffset(string code, int cursorOffset)
        {
            int line = 0;
            int character = 0;
            for (int i = 0; i < cursorOffset && i < code.Length; i++)
            {
                switch (code[i])
                {
                    case '\n':
                        line++;
                        character = 0;
                        break;
                    default:
                        character++;
                        break;
                }
            }

            return new LinePosition(line, character);
        }

        public static LinePositionSpan GetLinePositionSpanFromStartAndEndIndices(string code, int startIndex, int endIndex)
        {
            var start = GetPositionFromCursorOffset(code, startIndex);
            var end = GetPositionFromCursorOffset(code, endIndex);
            return new LinePositionSpan(start, end);
        }
    }
}
