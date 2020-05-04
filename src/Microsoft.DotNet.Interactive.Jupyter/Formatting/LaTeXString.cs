// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Jupyter.Formatting
{
    public class LaTeXString
    {
        private readonly string _latexCode;

        public LaTeXString(string latexCode)
        {
            _latexCode = latexCode ?? throw new ArgumentNullException(nameof(latexCode));
        }

        public static implicit operator LaTeXString(string source) => new LaTeXString(source);

        public override string ToString()
        {
            return _latexCode;
        }
    }
}