// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Commands;

[DebuggerStepThrough]
public abstract class KernelCommand
{

    protected KernelCommand(
        string targetKernelName = null, 
        KernelCommand parent = null)
    {
        Properties = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        TargetKernelName = targetKernelName;
        Parent = parent;
        RoutingSlip = new RoutingSlip();
    }

    [JsonIgnore] 
    public KernelCommandInvocation Handler { get; set; }

    [JsonIgnore]
    public KernelCommand Parent { get; internal set; }

    [JsonIgnore]
    public IDictionary<string, object> Properties { get; }

    public string TargetKernelName { get; internal set; }

    internal static KernelCommand None => new NoCommand();

    public Uri OriginUri { get; set; }

    public Uri DestinationUri { get; set; }

    [JsonIgnore]
    internal SchedulingScope SchedulingScope { get; set; } 

    [JsonIgnore]
    internal bool? ShouldPublishCompletionEvent { get; set; }

    [JsonIgnore] 
    public ParseResult KernelChooserParseResult { get; internal set; }

    [JsonIgnore]
    public RoutingSlip RoutingSlip
    {
        get;
    }

    public virtual Task InvokeAsync(KernelInvocationContext context)
    {
        if (Handler is null)
        {
            throw new NoSuitableKernelException(this);
        }

        return Handler(this, context);
    }

    public bool TryAddToRoutingSlip(Uri uri)
    {
        if (Parent?.RoutingSlip.Contains(uri) == true)
        {
            return false;
        }
        return RoutingSlip.TryAdd(uri);
    }
}