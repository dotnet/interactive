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
public abstract class KernelCommand
{
    private KernelCommand _parent;

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
            _parent = value;
            //
            // if (value is { } )
            // {
            //     if (Token is null)
            //     {
            //         this.SetToken(value.GetOrCreateToken());
            //     }
            // }
            // else
            // {
            //     // FIX: (Parent) should this be allowed?
            // }
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
        if (!Properties.TryGetValue(TokenKey, out var existing))
        {
            Properties.Add(TokenKey, new TokenSequence(token));
        }
        else if (existing is not TokenSequence sequence || sequence.Current != token)
        {
            throw new InvalidOperationException("Command token cannot be changed.");
        }
    }

    internal const string TokenKey = "token";

    public string GetOrCreateToken()
    {
        // FIX: (GetOrCreateToken) make this a property
        if (Properties.TryGetValue(TokenKey, out var value) &&
            value is TokenSequence tokenSequence)
        {
            return tokenSequence.Current;
        }

        if (Parent is { } parent)
        {
            var token = parent.GetOrCreateToken();
            SetToken(token);
            return token;
        }

        // FIX: (GetOrCreateToken) don't depend on KernelInvocationContext.Current
        if (KernelInvocationContext.Current?.Command is { } contextCommand &&
            !CommandEqualityComparer.Instance.Equals(contextCommand, this))
        {
            var token = contextCommand.GetOrCreateToken();
            SetToken(token);
            return token;
        }

        return GenerateToken();
    }

    private string GetNextToken()
    {
        if (Properties.TryGetValue(TokenKey, out var value) &&
            value is TokenSequence tokenSequence)
        {
            return tokenSequence.GetNext();
        }

        return GenerateToken();
    }

    private string GenerateToken()
    {
        var seed = Parent?.GetNextToken();

        var sequence = new TokenSequence(seed);

        Properties.Add(TokenKey, sequence);

        return sequence.Current;
    }

    private class TokenSequence
    {
        private readonly object _lock = new();

        public TokenSequence(string? current = null)
        {
            Current = current ?? Hash(Guid.NewGuid().ToString());
        }

        internal string Current { get; private set; }

        public string GetNext()
        {
            string next;

            lock (_lock)
            {
                next = Current = Hash(Current);
            }

            return next;
        }

        private static string Hash(string seed)
        {
            var inputBytes = Encoding.ASCII.GetBytes(seed);

            byte[] hash;
            using (var sha = SHA256.Create())
            {
                hash = sha.ComputeHash(inputBytes);
            }

            return Convert.ToBase64String(hash);
        }
    }

    [JsonIgnore]
    internal SchedulingScope SchedulingScope { get; set; }

    [JsonIgnore]
    internal bool? ShouldPublishCompletionEvent { get; set; }

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
}