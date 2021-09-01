// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class NotSerializableAttribute : Attribute
    {
        public string Message { get; }

        public NotSerializableAttribute(string message)
        {
            Message = message;
        }
    }
}