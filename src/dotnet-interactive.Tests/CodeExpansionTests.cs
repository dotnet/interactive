// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.Commands;
using Microsoft.DotNet.Interactive.App.Events;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using static Microsoft.DotNet.Interactive.App.CodeExpansionInfo;

namespace Microsoft.DotNet.Interactive.App.Tests;

public class CodeExpansionTests
{
    [Fact]
    public async Task Connection_shortcuts_include_well_known_data_connections()
    {
        using var kernel = CreateKernel().UseCodeExpansions(() => new RecentConnectionList(), list => { });

        var result = await kernel.SendAsync(new RequestCodeExpansionInfos());

        result.Events.Should().NotContainErrors();

        result.Events.Should().ContainSingle<CodeExpansionInfosProduced>()
              .Which
              .CodeExpansionInfos
              .Should()
              .Contain([
                  new CodeExpansionInfo("Kusto Query Language", CodeExpansionKind.WellKnownConnection),
                  new CodeExpansionInfo("Microsoft SQL Database", CodeExpansionKind.WellKnownConnection),
              ]);
    }

    [Fact]
    public async Task When_connect_directive_is_run_then_recent_connection_list_item_is_added()
    {
        RecentConnectionList recentConnectionList = new();

        using var kernel = CreateKernel()
            .UseCodeExpansions(() => recentConnectionList, list => { });

        kernel.AddConnectDirective(
            new ConnectFakeKernelDirective("fake", name => Task.FromResult<Kernel>(new FakeKernel(name))));

        await kernel.SendAsync(new SubmitCode("#!connect fake --kernel-name myFakeKernel --fakeness-level 9000"));

        using var kernel2 = CreateKernel()
            .UseCodeExpansions(() => recentConnectionList, list => { });

        var result = await kernel2.SendAsync(new RequestKernelInfo());

        result.Events
              .OfType<KernelInfoProduced>()
              .Should()
              .ContainSingle(e => e.KernelInfo.LocalName == "myFakeKernel")
            //.Which
            //.KernelInfo
            ;

        throw new NotImplementedException();
    }

    [Fact]
    public void When_connect_directive_comes_from_nuget_package_then_pound_r_is_included_in_the_connection_shortcut()
    {
        // TODO (When_connect_directive_comes_from_nuget_package_then_pound_r_is_included_in_the_connection_shortcut) write test
        throw new NotImplementedException();
    }

    [Fact]
    public void Triggering_a_code_expansion_causes_a_SendEditableCode_to_be_sent_with_the_connect_code()
    {
        // TODO (Triggering_a_connection_shortcut_causes_a_SendEditableCode_to_be_sent) write test
        throw new NotImplementedException();
    }

    private static CompositeKernel CreateKernel()
    {
        var pwshKernel = new PowerShellKernel();
        return new CompositeKernel
        {
            new CSharpKernel(),
            pwshKernel
        }.UseSecretManager(new SecretManager(pwshKernel));
    }
}