// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Microsoft.DotNet.Interactive.Commands;

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Events
{
    public class KernelExtensionLoaded : KernelEvent
    {
        public string ExtensionType { get; }

        [JsonIgnore]
        public IKernelExtension KernelExtension { get; }

        [JsonConstructor]
        public KernelExtensionLoaded(string extensionType, string contentSourceName, bool isContentProvider ,KernelCommand command = null) : base(command)
        {
            ExtensionType = extensionType;
            ContentSourceName = contentSourceName;
            IsContentProvider = isContentProvider;
        }
        public KernelExtensionLoaded(IKernelExtension kernelExtension, KernelCommand command = null) : base(command)
        {
            KernelExtension = kernelExtension;
            ExtensionType = kernelExtension.GetType().Name;

            if (kernelExtension is IStaticContentSource contentSource)
            {
                IsContentProvider = true;
                ContentSourceName = contentSource.Name;
            }

        }

        public string ContentSourceName { get; }

        public bool IsContentProvider { get;  }

        public override string ToString() => $"{base.ToString()}: {ExtensionType}";
    }
}