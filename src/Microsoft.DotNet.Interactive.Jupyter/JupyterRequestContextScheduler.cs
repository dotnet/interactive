// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;


namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class JupyterRequestContextScheduler
    {
        private Func<JupyterRequestContext, Task> handle;

        public JupyterRequestContextScheduler(Func<JupyterRequestContext, Task> handle)
        {
            this.handle = handle ?? throw new ArgumentNullException(nameof(handle));
        }

        public async Task Schedule(JupyterRequestContext context)
        {
            await handle(context);
        }
    }
}