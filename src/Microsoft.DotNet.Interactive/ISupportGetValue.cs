// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive
{
    public interface ISupportGetValue
    {
        public bool TryGetValue<T>(string name, out T value);

        public IReadOnlyCollection<string> GetValueNames();
    }
}