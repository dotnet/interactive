// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public abstract class DotNetKernel : Kernel
    {
        protected DotNetKernel(string name) : base(name)
        {
        }

        public abstract bool TryGetVariable<T>(string name, out T value);

        public abstract Task SetVariableAsync(string name, object value, Type declaredType = null);

        public abstract IReadOnlyCollection<string> GetVariableNames();
    }
}