// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipes;
using System.Reactive.Subjects;

namespace Microsoft.DotNet.Interactive
{
    internal class OutputPipeStream
    {
        private readonly Subject<string> _subject = new Subject<string>();
        private readonly PipeStream _output;

        public OutputPipeStream(PipeStream output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public IObservable<string> OutputObservable => _subject;

        public void Write(string text)
        {
            _output.WriteMessage(text);
            _output.Flush();
            _subject.OnNext(text);
        }
    }
}