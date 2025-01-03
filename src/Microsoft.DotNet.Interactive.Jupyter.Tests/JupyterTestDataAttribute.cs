// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit.Sdk;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal class JupyterTestDataAttribute : DataAttribute
{
    private readonly object[] _data;
    public const string JUPYTER_RECORDED_TEST = nameof(JUPYTER_RECORDED_TEST);

    public JupyterTestDataAttribute(params object[] data)
    {
        _data = data;
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        List<object> testData = new();
        var connData = GetConnectionTestData();
        connData.KernelSpecName = KernelSpecName;
        testData.Add(connData);
        testData.AddRange(_data);
        return new[] { testData.ToArray() };
    }
    protected virtual JupyterConnectionTestData GetConnectionTestData()
    {
        return new JupyterConnectionTestData(JUPYTER_RECORDED_TEST);
    }

    public string KernelSpecName { get; set; }
}