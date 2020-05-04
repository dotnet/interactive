// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Subjects;

namespace Microsoft.DotNet.Interactive
{
    internal class OutputTextStream
    {
        private readonly Subject<string> _subject = new Subject<string>();
        private readonly TextWriter _output;

        public OutputTextStream(TextWriter output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public IObservable<string> OutputObservable => _subject;

        public void Write(string text)
        {
            _output.WriteLine(text);
            _output.Flush();
            _subject.OnNext(text);
        }
    }
}