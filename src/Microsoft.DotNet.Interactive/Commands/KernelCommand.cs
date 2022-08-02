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
    private readonly RoutingSlip _routingSlip;

    protected KernelCommand(
        string targetKernelName = null, 
        KernelCommand parent = null)
    {
        Properties = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        TargetKernelName = targetKernelName;
        Parent = parent;
        _routingSlip = new RoutingSlip();
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
    public IReadOnlyCollection<Uri> RoutingSlip
    {
        get
        {
            var unique = new HashSet<Uri>();
            var list = new List<Uri>(Parent?.RoutingSlip ?? Array.Empty<Uri>());
            list.AddRange(_routingSlip.Where(unique.Add));
            return list;
        }
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
        return _routingSlip.TryAdd(uri);
    }
}