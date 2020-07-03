// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public abstract class DotNetLanguageKernel : Kernel
    {
        protected DotNetLanguageKernel(string name) : base(name)
        {
        }

        public abstract bool TryGetVariable<T>(string name, out T value);

        public abstract Task SetVariableAsync(string name, object value);

        public abstract IReadOnlyCollection<string> GetVariableNames();
    }
}