// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;

namespace Microsoft.DotNet.Interactive.Tests.Connection;

[TestClass]
public class KernelHostTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public KernelHostTests(TestContext output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
    }

    [TestMethod]
    public void When_kernel_is_added_to_hosted_CompositeKernel_then_origin_URI_is_set()
    {
        using var composite = new CompositeKernel();
        composite.ConnectInProcessHost();

        var kernel = new FakeKernel("fake");

        composite.Add(kernel);

        var kernelInfo = kernel.KernelInfo;

        kernelInfo.Uri.Should().Be(new Uri(composite.Host.Uri, "fake"));
    }

    [TestMethod]
    public void It_does_not_throw_when_proxy_kernel_is_created_for_nonexistent_remote()
    {
        using var localCompositeKernel = new CompositeKernel("LOCAL");
        using var remoteCompositeKernel = new CompositeKernel("REMOTE");

        ConnectHost.ConnectInProcessHost(
            localCompositeKernel,
            remoteCompositeKernel);

        var remoteKernelUri = new Uri("kernel://DOES/NOT/EXIST");

        localCompositeKernel
            .Invoking(async k =>
                          await k.Host
                                 .ConnectProxyKernelOnDefaultConnectorAsync(
                                     "fsharp",
                                     remoteKernelUri))
            .Should().NotThrowAsync();
    }
        
    public void Dispose() => _disposables.Dispose();
}
