// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection;

public delegate CommandOrEvent ReadCommandOrEvent(CancellationToken cancellationToken = default);

public delegate Task<CommandOrEvent> ReadCommandOrEventAsync(CancellationToken cancellationToken = default);
