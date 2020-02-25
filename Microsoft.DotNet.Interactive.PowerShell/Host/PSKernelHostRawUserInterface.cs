// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Management.Automation.Host;

namespace Microsoft.DotNet.Interactive.PowerShell.Host
{
    public class PSKernelHostRawUserInterface : PSHostRawUserInterface
    {
        private Size _bufferSize;
        private ConsoleColor _foregroundColor, _backgroundColor;

        internal PSKernelHostRawUserInterface()
        {
            _bufferSize = new Size(100, 50);
            _foregroundColor = VTColorUtils.DefaultConsoleColor;
            _backgroundColor = VTColorUtils.DefaultConsoleColor;
        }

        public override ConsoleColor ForegroundColor
        {
            get => _foregroundColor;
            set => _foregroundColor = value;
        }

        public override ConsoleColor BackgroundColor
        {
            get => _backgroundColor;
            set => _backgroundColor = value;
        }

        public override Size BufferSize { get => _bufferSize; set => _bufferSize = value; }
        public override Size WindowSize { get => _bufferSize; set => _bufferSize = value; }
        public override Size MaxPhysicalWindowSize => _bufferSize;
        public override Size MaxWindowSize => _bufferSize;

        #region "LengthInBufferCells"

        public override int LengthInBufferCells(string source, int offset)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (offset < 0 || offset > source.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            int length = 0;
            for (int i = offset; i < source.Length; i++)
            {
                length += LengthInBufferCells(source[i]);
            }

            return length;
        }

        public override int LengthInBufferCells(string source)
        {
            return LengthInBufferCells(source, offset: 0);
        }

        public override int LengthInBufferCells(char c)
        {
            // The following is based on http://www.cl.cam.ac.uk/~mgk25/c/wcwidth.c
            // which is derived from https://www.unicode.org/Public/UCD/latest/ucd/EastAsianWidth.txt
            bool isWide = c >= 0x1100 &&
                (c <= 0x115f || /* Hangul Jamo init. consonants */
                 c == 0x2329 || c == 0x232a ||
                 ((uint)(c - 0x2e80) <= (0xa4cf - 0x2e80) &&
                  c != 0x303f) || /* CJK ... Yi */
                 ((uint)(c - 0xac00) <= (0xd7a3 - 0xac00)) || /* Hangul Syllables */
                 ((uint)(c - 0xf900) <= (0xfaff - 0xf900)) || /* CJK Compatibility Ideographs */
                 ((uint)(c - 0xfe10) <= (0xfe19 - 0xfe10)) || /* Vertical forms */
                 ((uint)(c - 0xfe30) <= (0xfe6f - 0xfe30)) || /* CJK Compatibility Forms */
                 ((uint)(c - 0xff00) <= (0xff60 - 0xff00)) || /* Fullwidth Forms */
                 ((uint)(c - 0xffe0) <= (0xffe6 - 0xffe0)));

            // We can ignore these ranges because .Net strings use surrogate pairs
            // for this range and we do not handle surrogage pairs.
            // (c >= 0x20000 && c <= 0x2fffd) ||
            // (c >= 0x30000 && c <= 0x3fffd)
            return 1 + (isWide ? 1 : 0);
        }

        #endregion

        #region "NotSupported Members"

        private const string NotSupportedFeatureMsg = "This method or property is not supported by the PowerShell kernel.";

        public override int CursorSize
        {
            get => throw new NotSupportedException(NotSupportedFeatureMsg);
            set => throw new NotSupportedException(NotSupportedFeatureMsg);
        }

        public override Coordinates CursorPosition
        {
            get => throw new NotSupportedException(NotSupportedFeatureMsg);
            set => throw new NotSupportedException(NotSupportedFeatureMsg);
        }

        public override bool KeyAvailable => throw new NotSupportedException(NotSupportedFeatureMsg);

        public override Coordinates WindowPosition
        {
            get => throw new NotSupportedException(NotSupportedFeatureMsg);
            set => throw new NotSupportedException(NotSupportedFeatureMsg);
        }

        public override string WindowTitle
        {
            get => throw new NotSupportedException(NotSupportedFeatureMsg);
            set => throw new NotSupportedException(NotSupportedFeatureMsg);
        }

        public override void FlushInputBuffer()
        {
            throw new NotSupportedException(NotSupportedFeatureMsg);
        }

        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            throw new NotSupportedException(NotSupportedFeatureMsg);
        }

        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            throw new NotSupportedException(NotSupportedFeatureMsg);
        }

        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
            throw new NotSupportedException(NotSupportedFeatureMsg);
        }

        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            throw new NotSupportedException(NotSupportedFeatureMsg);
        }

        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            throw new NotSupportedException(NotSupportedFeatureMsg);
        }

        #endregion
    }
}
