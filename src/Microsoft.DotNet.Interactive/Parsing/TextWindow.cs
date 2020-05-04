// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.DotNet.Interactive.Parsing
{
    [DebuggerStepThrough]
    internal class TextWindow
    {
        public TextWindow(int start, int limit)
        {
            Start = start;
            Limit = limit;
            End = start;
        }

        public int Start { get; }

        public int End { get; private set; }
        
        public int Limit { get; }

        public int Length => End - Start;

        public bool IsEmpty => Start == End;

        public void Advance()
        {
            End++;

#if DEBUG
            if (End > Limit)
            {
                throw new InvalidOperationException();
            }
#endif
        }

        public TextSpan Span => new TextSpan(Start, Length);

        public override string ToString() => $"[{Start}..{End}]";
    }
}