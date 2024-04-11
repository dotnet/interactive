// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.CSharpProject.Build;

public class PrebuildDescriptor
{
    public PrebuildDescriptor(
        string name, 
        string version = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        Name = name;
        Version = version;
        IsPathSpecified = name.Contains("\\") || name.Contains("/");
    }

    public string Name { get; }

    public string Version { get; }

    internal bool IsPathSpecified { get; }

    public override bool Equals(object obj)
    {
        return obj is PrebuildDescriptor descriptor &&
               Name == descriptor.Name &&
               Version == descriptor.Version &&
               IsPathSpecified == descriptor.IsPathSpecified;
    }

    public override int GetHashCode()
    {
        var hashCode = -858720875;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Version);
        hashCode = hashCode * -1521134295 + IsPathSpecified.GetHashCode();
        return hashCode;
    }

    public override string ToString() => Name;
}