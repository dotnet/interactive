// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Commands;

[DebuggerStepThrough]
public abstract class KernelCommand : IEquatable<KernelCommand>
{
    private KernelCommand _parent;
    private string _token;

    protected KernelCommand(
        string targetKernelName = null,
        KernelCommand parent = null)
    {
        Properties = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        TargetKernelName = targetKernelName;
        RoutingSlip = new CommandRoutingSlip();
        if (parent is { })
        {
            Parent = parent;
        }
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

    public string GetOrCreateToken()
    {
        if (_token is not null)
        {
            return _token;
        }

        if (Parent is { } parent)
        {
            _token = parent._token;
            return _token;
        }

        // FIX: (GetOrCreateToken) don't depend on KernelInvocationContext.Current
        if (KernelInvocationContext.Current?.Command is { } contextCommand &&
            !Equals(contextCommand))
        {
            var token = contextCommand.GetOrCreateToken();
            SetToken(token);
            return token;
        }

        _token = CreateToken();

        return _token;

        static string CreateToken()
        {
            var inputBytes = Encoding.ASCII.GetBytes(Guid.NewGuid().ToString());

            byte[] hash;
            using (var sha = SHA256.Create())
            {
                hash = sha.ComputeHash(inputBytes);
            }

            return Convert.ToBase64String(hash);
        }
    }

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

    internal const string IdKey = "id";

    internal void SetId(string id)
    {
        // FIX: (SetId) don't use Properties for these
        Properties[IdKey] = id;
    }

    internal string GetOrCreateId()
    {
        if (Properties.TryGetValue(IdKey, out var value))
        {
            return (string)value;
        }

        var id = Guid.NewGuid().ToString("N");
        SetId(id);
        return id;
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
}