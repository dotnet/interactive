// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Pocket;

namespace Microsoft.DotNet.Interactive.AIUtilities;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class InteractiveExtension
{
    static InteractiveExtension()
    {
        _logger = new Logger(typeof(Text).FullName);
    }

    private static readonly Logger _logger;

    public static void Load()
    {
        _logger.Event();
    }
}