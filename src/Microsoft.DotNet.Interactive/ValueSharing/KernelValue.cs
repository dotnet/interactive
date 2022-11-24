// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.ValueSharing;

public class KernelValue
{
    private readonly KernelValueInfo _valueInfo;

    public KernelValue(KernelValueInfo valueInfo, object value, string kernelName)
    {
        _valueInfo = valueInfo;
        Value = value;
        KernelName = kernelName;
    }

    public object Value { get; }

    public Type Type => _valueInfo.Type;

    public string Name => _valueInfo.Name;

    public string KernelName { get; }
}