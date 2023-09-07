// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Threading;

namespace Microsoft.DotNet.Interactive.Commands;

public abstract class KernelCommand : IEquatable<KernelCommand>
{
    private KernelCommand _parent;
    private string _token;
    private string _id;

    protected KernelCommand(
        string targetKernelName = null)
    {
        Properties = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        TargetKernelName = targetKernelName;
        RoutingSlip = new CommandRoutingSlip();
    }

    [JsonIgnore]
    public KernelCommandInvocation Handler { get; set; }

    [JsonIgnore]
    public KernelCommand Parent
    {
        get => _parent;
        internal set
        {
            if (_parent is null)
            {
                _parent = value;
            }
            else if (_parent != value)
            {
                throw new InvalidOperationException("Parent cannot be changed.");
            }
        }
    }

    [JsonIgnore]
    public IDictionary<string, object> Properties { get; }

    public string TargetKernelName { get; internal set; }

    internal static KernelCommand None => new NoCommand();

    public Uri OriginUri { get; set; }

    public Uri DestinationUri { get; set; }

    public void SetToken(string token)
    {
        if (_token is null)
        {
            _token = token;
        }
        else if (token != _token)
        {
            throw new InvalidOperationException("Command token cannot be changed.");
        }
    }

    private int _childTokenCounter;

    private int GetNextChildToken()
    {
        return Interlocked.Increment(ref _childTokenCounter);
    }

    public string GetOrCreateToken()
    {
        if (_token is not null)
        {
            return _token;
        }

        if (_parent is { })
        {
            _token = $"{_parent.GetOrCreateToken()}.{_parent.GetNextChildToken()}";
            return _token;
        }

        _token = CreateToken();

        return _token;

        static string CreateToken()
        {
#if DEBUG
            var token = Interlocked.Increment(ref _nextRootToken);
            return token.ToString();
#else
            var inputBytes = Guid.NewGuid().ToByteArray();
            return Convert.ToBase64String(inputBytes);
#endif
        }
    }

#if DEBUG
    private static int _nextRootToken = 0;
#endif

    [JsonIgnore] internal SchedulingScope SchedulingScope { get; set; }

    [JsonIgnore] internal bool? ShouldPublishCompletionEvent { get; set; }

    [JsonIgnore]
    public ParseResult KernelChooserParseResult { get; internal set; }

    [JsonIgnore]
    public CommandRoutingSlip RoutingSlip { get; }

    public virtual Task InvokeAsync(KernelInvocationContext context)
    {
        if (Handler is null)
        {
            throw new NoSuitableKernelException(this);
        }

        return Handler(this, context);
    }

    public void SetId(string id)
    {
        _id = id;
    }

    internal string GetOrCreateId()
    {
        if (_id is not null)
        {
            return _id;
        }

        SetId(Guid.NewGuid().ToString("N"));

        return _id;
    }

    public bool Equals(KernelCommand other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }

        return GetOrCreateId() == other.GetOrCreateId();
    }

    public override int GetHashCode()
    {
        return GetOrCreateId().GetHashCode();
    }

    internal bool IsSelfOrDescendantOf(KernelCommand other)
    {
        return GetOrCreateToken().StartsWith(other.GetOrCreateToken());
    }

    internal bool HasSameRootCommandAs(KernelCommand other)
    {
        var thisParentToken = GetRootToken(GetOrCreateToken());
        var otherParentToken = GetRootToken(other.GetOrCreateToken());

        return thisParentToken == otherParentToken;
    }

    internal static string GetRootToken(string token)
    {
        var parts = token.Split(new[] { '.' });

        return parts[0];
    }
}