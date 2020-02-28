// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive
{
    public class VariableNotFoundException : Exception
    {
        public VariableNotFoundException(string variableName) : base($"Variable named {variableName} cannot be found")
        {

        }
    }
}