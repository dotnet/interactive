// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.CSharp;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests;

public class RuntimeTelemetryTests
{
    [Fact]
    public void Language_information_is_sent_on_cell_execution()
    {
        using var kernel = new CSharpKernel();




        // TODO (Language_information_is_sent_on_cell_execution) write test
        throw new NotImplementedException();
    }

    [Fact]
    public void Package_and_version_number_are_sent_on_cell_execution()
    {
        // TODO (Package_and_version_number_are_sent_on_cell_execution) write test
        throw new NotImplementedException();
    }
}