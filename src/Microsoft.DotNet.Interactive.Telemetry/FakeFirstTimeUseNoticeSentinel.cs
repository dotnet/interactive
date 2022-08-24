// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Telemetry;

public sealed class FakeFirstTimeUseNoticeSentinel : IFirstTimeUseNoticeSentinel
{
    public bool SentinelExists { get; set; }

    public void CreateIfNotExists()
    {
        SentinelExists = true;
    }

    public bool Exists() => SentinelExists;
}