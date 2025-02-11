﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events;

[DebuggerStepThrough]
public class KernelInfoProduced : KernelEvent
{
    public KernelInfoProduced(
        KernelInfo kernelInfo,
        KernelCommand command) : base(command)
    {
        KernelInfo = kernelInfo;
    }

    public KernelInfo KernelInfo { get; }

    // FIX: (KernelInfoProduced) can we make these non-public?

    [JsonIgnore]
    public string ConnectionShortcutCode { get; internal set; }

    [JsonIgnore]
    public Assembly ConnectionSourceAssembly { get; internal set; }
}