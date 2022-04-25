// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.CSharp;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class InputsWithinMagicCommandsTests
{


    [Fact]
    public void Input_token_in_magic_command_prompts_user_for_input()
    {


        using var kernel = CreateKernel();





        

        // TODO (testname) write test
        throw new NotImplementedException();
    }

    [Fact]
    public void Input_token_is_stored_when_used_in_value_kernel()
    {






        

        // TODO (Input_token_is_stored_when_used_in_KeyValueStore_kernel) write test
        throw new NotImplementedException();
    }


    private static CompositeKernel CreateKernel() =>
        new()
        {
            new CSharpKernel()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseValueSharing(),
            new KeyValueStoreKernel("value-kernel")
        };



}