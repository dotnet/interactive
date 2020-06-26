// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive
{
    public class TextWriterOutputStream : OutputTextStream
    {
        private readonly TextWriter _output;

        public TextWriterOutputStream(TextWriter output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        protected override void WriteText(string text)
        {
            _output.WriteLine(text);
            _output.Flush();
        }
    }
}