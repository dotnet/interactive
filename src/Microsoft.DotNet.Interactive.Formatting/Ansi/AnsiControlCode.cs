﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.DotNet.Interactive.Formatting.Ansi
{
    [DebuggerStepThrough]
    public class AnsiControlCode
    {
        public AnsiControlCode(string escapeSequence)
        {
            if (string.IsNullOrWhiteSpace(escapeSequence))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(escapeSequence));
            }

            EscapeSequence = escapeSequence;
        }

        public string EscapeSequence { get; }

        public override string ToString() => "";

        protected bool Equals(AnsiControlCode other) => string.Equals(EscapeSequence, other.EscapeSequence);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() &&
                   Equals((AnsiControlCode)obj);
        }

        public override int GetHashCode() => EscapeSequence.GetHashCode();

        public static implicit operator AnsiControlCode(string sequence)
        {
            return new AnsiControlCode(sequence);
        }
    }
}
