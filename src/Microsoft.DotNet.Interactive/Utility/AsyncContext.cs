// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Threading;
using Pocket;

namespace Microsoft.DotNet.Interactive.Utility
{
    public static class AsyncContext
    {
        private static int _seed = 0;

        private static readonly AsyncLocal<int?> _id = new AsyncLocal<int?>();
        private static Pocket.Logger _log = new("AsyncContext");

        public static int? Id
        {
            get => _id.Value;
            set => _id.Value = value;
        }

        public static bool TryEstablish(out int id, [CallerMemberName] string caller = null)
        {
            if (_id.Value is { } value)
            {
                id = _id.Value.Value;

                _log.Info("Caller {caller} on already-established context ID {id}", caller, id);

                return false;
            }
            else
            {
                _id.Value = Interlocked.Increment(ref _seed);
                id = _id.Value.Value;
                _log.Info("Caller {caller} established context ID {id}", caller, id);
                return true;
            }
        }
    }
}