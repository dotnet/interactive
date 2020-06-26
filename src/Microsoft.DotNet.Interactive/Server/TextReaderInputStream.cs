// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Server
{
    internal class TextReaderInputStream : InputTextStream
    {
        private readonly TextReader _input;
        public TextReaderInputStream(TextReader input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
        }

        protected override async Task<string> ReadLineAsync()
        {
            var line = await _input.ReadLineAsync();
            return line;
        }
    }
}