// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.DotNet.Interactive
{
    public class StaticContent
    {
        public void Map(string alias,  DirectoryInfo source)
        {

        }
    }

    public class KernelEnvironment
    {
        private Dictionary<string, Func<IKernel, string, object>> _map = new Dictionary<string, Func<IKernel, string, object>>();
        public void RegisterKernel<T>(T kernel, Func<T, string, object> environmentAccessor) where T: IKernel
        {
            if (kernel == null) throw new ArgumentNullException(nameof(kernel));
            if (environmentAccessor == null) throw new ArgumentNullException(nameof(environmentAccessor));
            
            _map[kernel.Name] = (k, symbolName) => environmentAccessor((T) k, symbolName);
        }
    }
}
