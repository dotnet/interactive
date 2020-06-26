// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public class PipeStreamInputStream : InputTextStream
    {
        private readonly PipeStream _input;
        public PipeStreamInputStream(PipeStream input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
        }

        protected override async Task<string> ReadLineAsync()
        {
            var message = await _input.ReadMessageAsync();
            return message;
        }
    }
}