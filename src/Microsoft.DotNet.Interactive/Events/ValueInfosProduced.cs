// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class ValueInfosProduced : KernelEvent
    {
        public IReadOnlyCollection<KernelValueInfo> ValueInfos { get; }

        public ValueInfosProduced(IReadOnlyCollection<KernelValueInfo> valueInfos, RequestValueInfos command) : base(command)
        {
            ValueInfos = valueInfos;
        }
    }
}
