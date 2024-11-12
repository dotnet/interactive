// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Documents;

public class InputField
{
    public InputField(string valueName, string typeHint = "text", string? prompt = null)
    {
        ValueName = valueName;
        Prompt = prompt ?? valueName;
        TypeHint = typeHint ?? "text";
    }

    public string TypeHint { get; set; }

    public string Prompt { get; }

    public string ValueName { get; set; }

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

        if (obj is InputField other)
        {
            return ValueName == other.ValueName && TypeHint == other.TypeHint;
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ValueName, TypeHint);
    }
}