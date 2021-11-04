// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class NonDestructurer : IDestructurer
    {
        private static readonly ICollection<string> _keys = new ReadOnlyCollection<string>(new[] { "value" });

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

        public ICollection<string> Keys => _keys;
    }
}