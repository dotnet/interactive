// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class NonDestructurer : IDestructurer
    {
        private NonDestructurer()
        {
        }

        public static IDestructurer Instance { get; } = new NonDestructurer();

        public IDictionary<string, object> Destructure(object instance)
        {
            return new Dictionary<string, object>
            {
                ["value"] = instance
            };
        }
    }
}