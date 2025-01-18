// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.App.Connection;

public record ConnectionShortcut(
    string Name,
    IReadOnlyList<string> ConnectCode,
    string Kind = null,
    string Description = null);