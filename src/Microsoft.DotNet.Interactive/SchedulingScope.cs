// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive;

internal class SchedulingScope
{
    private readonly string _stringValue;

    private SchedulingScope(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(scope));
        }

        Parts = scope.Split('/');
        _stringValue = scope;
    }

    protected bool Equals(SchedulingScope other)
    {
        return StringComparer.Ordinal.Equals(_stringValue, other._stringValue);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((SchedulingScope)obj);
    }

    public override int GetHashCode() => _stringValue.GetHashCode();

    public override string ToString()
    {
        return _stringValue;
    }

    public static SchedulingScope Parse(string scope) => new(scope);

    public string[] Parts { get; }

    public SchedulingScope Append(string part)
    {
        return new($"{_stringValue}/{part}");
    }

    public bool Contains(SchedulingScope scope)
    {
        if (scope.Parts.Length > Parts.Length)
        {
            return false;
        }

        for (var i = 0; i < scope.Parts.Length; i++)
        {
            if (!StringComparer.Ordinal.Equals(Parts[i], scope.Parts[i]))
            {
                return false;
            }
        }

        return true;
    }
}