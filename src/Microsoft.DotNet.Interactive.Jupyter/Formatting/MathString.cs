// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Jupyter.Formatting
{
    public class MathString
    {
        private readonly string _math;

        public MathString(string latexCode)
        {
            _math = latexCode ?? throw new ArgumentNullException(nameof(latexCode));
            if (!_math.StartsWith("$$"))
            {
                _math = $"$${_math}";
            }

            if (!_math.EndsWith("$$"))
            {
                _math = $"{_math}$$";
            }
        }

        public static implicit operator MathString(string source) => new MathString(source);

        public override string ToString()
        {
            return _math;
        }
    }
}