// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal interface IDestructurer
    {
        IDictionary<string, object> Destructure(object instance);
    }
}