// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.CSharp
{
    internal class Union<T1, T2>
    {
        private readonly int _case;

        public Union(T1 item1)
        {
            Item1 = item1;
            _case = 1;
        }

        public Union(T2 item2)
        {
            Item2 = item2;
            _case = 2;
        }

        public T1 Item1 { get; }

        public T2 Item2 { get; }

        public object Value =>
            _case switch
            {
                1 => Item1,
                2 => Item2,
                _ => throw new InvalidOperationException()
            };

        public static implicit operator Union<T1, T2>(T1 source) =>
            new Union<T1, T2>(source);

        public static implicit operator Union<T1, T2>(T2 source) =>
            new Union<T1, T2>(source);
    }
}