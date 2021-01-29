// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    public class KernelExtensionLoaded : KernelEvent
    {
        [JsonIgnore]
        public IKernelExtension KernelExtension { get; }


        [JsonConstructor]
        public KernelExtensionLoaded(KernelCommand command) : base(command)
        {
        }
        public KernelExtensionLoaded(IKernelExtension kernelExtension, KernelCommand command) : base(command)
        {
            KernelExtension = kernelExtension;

        }
    }
}