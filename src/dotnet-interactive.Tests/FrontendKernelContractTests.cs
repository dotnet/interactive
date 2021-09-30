// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.VSCode;
using Xunit;

#nullable enable

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class FrontendKernelContractTests
    {
        [Fact]
        public void GetInputAsync_contract_is_consistent()
        {
            var vscode = typeof(VSCodeInteractiveHost).GetMethod(nameof(VSCodeInteractiveHost.GetInputAsync));

            var jupyter = typeof(JupyterInteractiveHost).GetMethod(nameof(JupyterInteractiveHost.GetInputAsync));
            
            jupyter.ReturnType.Should().Be(vscode.ReturnType);
           
            GetParameters(jupyter)
                .Should()
                .BeEquivalentTo(GetParameters(vscode));

            (string? Name, Type ParameterType, bool IsOptional, object? DefaultValue)[] GetParameters(
                MethodInfo methodInfo)
            {
                return methodInfo.GetParameters()
                    .Select(p => (p.Name, p.ParameterType, p.IsOptional, p.DefaultValue))
                    .ToArray();
            }
        }

    }
}