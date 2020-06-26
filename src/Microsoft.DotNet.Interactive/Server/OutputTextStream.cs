// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Subjects;

namespace Microsoft.DotNet.Interactive.Server
{
    internal abstract class OutputTextStream : IOutputTextStream
    {
        private readonly Subject<string> _subject = new Subject<string>();
       

        public IObservable<string> OutputObservable => _subject;

        public void Write(string text)
        {
            WriteText(text);
            _subject.OnNext(text);
        }

        protected abstract void WriteText(string text);
    }
}