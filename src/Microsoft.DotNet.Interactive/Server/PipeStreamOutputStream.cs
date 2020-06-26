// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipes;

namespace Microsoft.DotNet.Interactive.Server
{
    internal class PipeStreamOutputStream : OutputTextStream
    {
        private readonly PipeStream _output;

        public PipeStreamOutputStream(PipeStream output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }
        protected override void WriteText(string text)
        {
            _output.WriteMessage(text);
            _output.Flush();
        }
    }
}