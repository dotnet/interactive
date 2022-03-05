// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Subjects;

namespace Microsoft.DotNet.Interactive.CSharpProject.WorkspaceFeatures
{
    public abstract class CommandLineObservable : IObservable<string>
    {
        private readonly ReplaySubject<string> _subject = new ReplaySubject<string>();

        internal void OnNext(string value) => _subject.OnNext(value);

        public IDisposable Subscribe(IObserver<string> observer) => _subject.Subscribe(observer);
    }
}