// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Threading;

namespace Microsoft.DotNet.Interactive.Commands;

[DebuggerStepThrough]
public abstract class KernelCommand : IEquatable<KernelCommand>
{
    private KernelCommand _parent;
    private List<KernelCommand> _childCommandsToBubbleEventsFrom;
    private KernelCommand _selfOrFirstUnhiddenAncestor;

    protected KernelCommand(string targetKernelName = null)
    {
        TargetKernelName = targetKernelName;
        RoutingSlip = new CommandRoutingSlip();
    }

    [JsonIgnore] 
    public KernelCommandInvocation Handler { get; set; }

    [JsonIgnore] 
    public KernelCommand Parent => _parent;

    internal string Token { get; private set; }

    public void SetParent(KernelCommand parent, bool bubbleEvents = false)
    {
        if (parent is null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        if (Token is not null &&
            parent.Token is not null && 
            GetRootToken(Token) != GetRootToken(parent.Token))
        {
            throw new InvalidOperationException("Token of parented command cannot be changed.");
        }

        if (_parent is null)
        {
            _parent = parent;

            if (_parent.Token is null)
            {
                _parent.GetOrCreateToken();
            }

            GetOrCreateToken();
        }
        else if (!_parent.Equals(parent))
        {
            throw new InvalidOperationException("Parent cannot be changed.");
        }

        if (bubbleEvents)
        {
            _parent.ResultShouldIncludeEventsFrom(this);
        }
    }

    [JsonInclude]
    public string TargetKernelName { get; internal set; }

    internal static KernelCommand None => new NoCommand();

    public Uri OriginUri { get; set; }

    public Uri DestinationUri { get; set; }

    public void SetToken(string token)
    {
        if (Token is null)
        {
            Token = token;
        }
        else if (token != Token)
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
        if (Token is not null)
        {
            return Token;
        }

        if (_parent is { })
        {
            Token = $"{_parent.GetOrCreateToken()}.{_parent.GetNextChildToken()}";
            return Token;
        }

        Token = CreateRootToken();

        return Token;

        static string CreateRootToken()
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

    [JsonIgnore] 
    internal SchedulingScope SchedulingScope { get; set; }

    [JsonIgnore] 
    internal bool? ShouldPublishCompletionEvent { get; set; }

    [JsonIgnore] public CommandRoutingSlip RoutingSlip { get; }

    internal bool WasProxied { get; set; }

    private void ResultShouldIncludeEventsFrom(KernelCommand childCommand)
    {
        if (_childCommandsToBubbleEventsFrom is null)
        {
            _childCommandsToBubbleEventsFrom = new();
        }

        _childCommandsToBubbleEventsFrom.Add(childCommand);
    }

    internal bool ShouldResultIncludeEventsFrom(KernelCommand childCommand)
    {
        if (WasProxied &&
            childCommand.IsSelfOrDescendantOf(this))
        {
            return true;
        }

        if (_childCommandsToBubbleEventsFrom is null)
        {
            return false;
        }

        for (var i = 0; i < _childCommandsToBubbleEventsFrom.Count; i++)
        {
            var command = _childCommandsToBubbleEventsFrom[i];

            if (command.Equals(childCommand))
            {
                return true;
            }

            if (command.WasProxied &&
                command.IsSelfOrDescendantOf(this))
            {
                return true;
            }
        }

        return false;
    }

    public virtual Task InvokeAsync(KernelInvocationContext context)
    {
        if (Handler is null)
        {
            throw new NoSuitableKernelException(this);
        }

        return Handler(this, context);
    }
    
    public bool Equals(KernelCommand other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Token is not null && other?.Token is not null)
        {
            var tokensAreEqual = Token == other.Token;

            return tokensAreEqual;
        }

        return false;
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

    internal virtual bool IsHidden => false;

    internal KernelCommand SelfOrFirstUnhiddenAncestor
    {
        get
        {
            if (_selfOrFirstUnhiddenAncestor is null)
            {
                var command = this;

                while (command.IsHidden)
                {
                    command = command.Parent;

                    if (command is null)
                    {
                        return null;
                    }
                }

                _selfOrFirstUnhiddenAncestor = command;
            }

            return _selfOrFirstUnhiddenAncestor;
        }
    }
}