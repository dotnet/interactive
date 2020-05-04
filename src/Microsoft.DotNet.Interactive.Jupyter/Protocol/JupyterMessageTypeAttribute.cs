// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class JupyterMessageTypeAttribute : Attribute
    {
        public string Type { get; }

        public JupyterMessageTypeAttribute(string messageType)
        {
            Type = messageType;
        }
    }
}