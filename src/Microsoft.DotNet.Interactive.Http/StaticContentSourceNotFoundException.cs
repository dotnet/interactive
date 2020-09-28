// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Http
{
    public class StaticContentSourceNotFoundException : Exception
    {
        public StaticContentSourceNotFoundException(string name):base($"Cannot find static content source {name}")
        {
            
        }
    }
}