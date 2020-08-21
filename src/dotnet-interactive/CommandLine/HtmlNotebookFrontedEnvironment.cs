// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.App.CommandLine
{
    public class HtmlNotebookFrontedEnvironment : BrowserFrontendEnvironment
    {
        private readonly TaskCompletionSource<Uri> _completionSource;

        public HtmlNotebookFrontedEnvironment()
        {
            _completionSource = new TaskCompletionSource<Uri>();
        }

        public HtmlNotebookFrontedEnvironment(Uri apiUri) : this()
        {
           SetApiUri(apiUri);
        }

        internal void SetApiUri(Uri apiUri)
        {
            _completionSource.TrySetResult(apiUri);
        }

        public Task<Uri> GetApiUriAsync()
        {
            return _completionSource.Task;
        }
    }
}