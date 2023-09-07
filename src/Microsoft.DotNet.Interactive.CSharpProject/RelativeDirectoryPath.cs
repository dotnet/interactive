// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class RelativeDirectoryPath :
    RelativePath,
    IEquatable<RelativeDirectoryPath>
{
    public static RelativeDirectoryPath Root { get; } = new("./");
      
    public RelativeDirectoryPath(string value) : base(value)
    {
        Value = NormalizeDirectory(value);
    }

    public bool Equals(RelativeDirectoryPath other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Equals(Value, other.Value);
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

        return Equals((RelativeDirectoryPath) obj);
    }

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(RelativeDirectoryPath left, RelativeDirectoryPath right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(RelativeDirectoryPath left, RelativeDirectoryPath right)
    {
        return !Equals(left, right);
    }
}