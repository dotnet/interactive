// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.ValueSharing;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RequestValue : KernelCommand
    {
        public string Name { get; }

        public string MimeType { get;  }

        public RequestValue(string name, string targetKernelName = null, string mimeType = null ) : base(targetKernelName)
        {
            Name = name;
            MimeType = mimeType ?? JsonFormatter.MimeType;
        }
    }
}