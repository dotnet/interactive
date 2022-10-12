// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class ValueAdapterCommandAttribute : Attribute
{
    public string Name { get; }

    public ValueAdapterCommandAttribute(string name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class ValueAdapterEventAttribute : Attribute
{
    public string Name { get; }

    public ValueAdapterEventAttribute(string name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class ValueAdapterMessageTypeAttribute : Attribute
{
    public string Name { get; }

    public ValueAdapterMessageTypeAttribute(string name)
    {
        Name = name;
    }
}
