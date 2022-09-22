// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Documents;

public class InputField
{
    public InputField(string valueName, string typeHint = "text")
    {
        ValueName = valueName;
        TypeHint = typeHint;
    }

    public string ValueName { get; set; }

    public string TypeHint { get; set; }

    protected bool Equals(InputField other)
    {
        return ValueName == other.ValueName && TypeHint == other.TypeHint;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((InputField)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ValueName, TypeHint);
    }
}