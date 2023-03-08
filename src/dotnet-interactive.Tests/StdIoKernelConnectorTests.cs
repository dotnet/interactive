using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.App.Connection;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class StdIoKernelConnectorTests
    {
        private static StdIoKernelConnector CreateConnector()
        {
            var pocketLoggerPath = Environment.GetEnvironmentVariable("POCKETLOGGER_LOG_PATH");
            string loggingArgs = null;

            if (File.Exists(pocketLoggerPath))
            {
                var logDir = Path.GetDirectoryName(pocketLoggerPath);
                loggingArgs = $"--verbose --log-path {logDir}";
            }

            var dotnetInteractive = typeof(Program).Assembly.Location;
            var hostUri = KernelHost.CreateHostUri("host");
            var connector = new StdIoKernelConnector(
                new[] { "dotnet", $""" "{dotnetInteractive}" stdio {loggingArgs}""" },
                rootProxyKernelLocalName: "rootProxy",
                hostUri);

            return connector;
        }

        [Fact]
        public async Task it_can_return_a_proxy_to_a_remote_composite()
        {
            var connector = CreateConnector();
            using var rootProxyKernel = await connector.CreateRootProxyKernelAsync();

            using var _ = new AssertionScope();
            rootProxyKernel.KernelInfo.IsProxy.Should().BeTrue();
            rootProxyKernel.KernelInfo.IsComposite.Should().BeTrue();
        }

        [Fact]
        public async Task it_can_create_a_proxy_to_a_specific_remote_subkernel()
        {
            var connector = CreateConnector();
            using var rootProxyKernel = await connector.CreateRootProxyKernelAsync();
            using var proxyKernel = await connector.CreateProxyKernelAsync(remoteName: "csharp");

            using var _ = new AssertionScope();
            proxyKernel.KernelInfo.IsProxy.Should().BeTrue();
            proxyKernel.KernelInfo.IsComposite.Should().BeFalse();
            proxyKernel.KernelInfo.LanguageName.Should().Be("C#");
            proxyKernel.Name.Should().Be("csharp");
        }

        [Fact]
        public async Task it_can_create_a_proxy_kernel_with_a_different_name_than_the_remote()
        {
            var connector = CreateConnector();
            using var rootProxyKernel = await connector.CreateRootProxyKernelAsync();
            using var proxyKernel = await connector.CreateProxyKernelAsync(remoteName: "fsharp", localNameOverride: "fsharp2");

            using var _ = new AssertionScope();
            proxyKernel.KernelInfo.IsProxy.Should().BeTrue();
            proxyKernel.KernelInfo.IsComposite.Should().BeFalse();
            proxyKernel.KernelInfo.LanguageName.Should().Be("F#");
            proxyKernel.Name.Should().Be("fsharp2");
        }

        [Fact]
        public async Task it_can_create_a_proxy_kernel_to_more_than_one_remote_subkernel()
        {
            var connector = CreateConnector();
            using var rootProxyKernel = await connector.CreateRootProxyKernelAsync();

            var result = await rootProxyKernel.SendAsync(new RequestKernelInfo());
            var kernelInfos = result.Events.OfType<KernelInfoProduced>().Select(e => e.KernelInfo);
            var csharpKernelInfo = kernelInfos.Should().ContainSingle(i => i.LanguageName == "C#").Which;
            var fsharpKernelInfo = kernelInfos.Should().ContainSingle(i => i.LanguageName == "F#").Which;

            using var csharpProxyKernel = await connector.CreateProxyKernelAsync(remoteInfo: csharpKernelInfo);
            using var fsharpProxyKernel = await connector.CreateProxyKernelAsync(remoteInfo: fsharpKernelInfo, localNameOverride: "fsharp2");

            using var _ = new AssertionScope();

            csharpProxyKernel.Name.Should().Be(csharpKernelInfo.LocalName);
            csharpProxyKernel.KernelInfo.DisplayName.Should().Be(csharpKernelInfo.DisplayName);
            csharpProxyKernel.KernelInfo.IsProxy.Should().BeTrue();
            csharpProxyKernel.KernelInfo.IsComposite.Should().BeFalse();
            csharpProxyKernel.KernelInfo.LanguageName.Should().Be(csharpKernelInfo.LanguageName);
            csharpProxyKernel.KernelInfo.LanguageVersion.Should().Be(csharpKernelInfo.LanguageVersion);
            csharpProxyKernel.KernelInfo.SupportedKernelCommands.Should().BeEquivalentTo(csharpKernelInfo.SupportedKernelCommands);
            csharpProxyKernel.KernelInfo.SupportedDirectives.Should().BeEquivalentTo(csharpKernelInfo.SupportedDirectives);

            fsharpProxyKernel.Name.Should().Be("fsharp2");
            fsharpProxyKernel.KernelInfo.DisplayName.Should().Be(fsharpKernelInfo.DisplayName);
            fsharpProxyKernel.KernelInfo.IsProxy.Should().BeTrue();
            fsharpProxyKernel.KernelInfo.IsComposite.Should().BeFalse();
            fsharpProxyKernel.KernelInfo.LanguageName.Should().Be(fsharpKernelInfo.LanguageName);
            fsharpProxyKernel.KernelInfo.LanguageVersion.Should().Be(fsharpKernelInfo.LanguageVersion);
            fsharpProxyKernel.KernelInfo.SupportedKernelCommands.Should().BeEquivalentTo(fsharpKernelInfo.SupportedKernelCommands);
            fsharpProxyKernel.KernelInfo.SupportedDirectives.Should().BeEquivalentTo(fsharpKernelInfo.SupportedDirectives);
        }

        [Fact]
        public async Task it_throws_if_proxy_to_the_remote_composite_is_not_created_before_creating_a_proxy_to_a_specific_remote_subkernel()
        {
            var connector = CreateConnector();
            var action = async () => await connector.CreateProxyKernelAsync(remoteName: "csharp");
            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task it_throws_if_there_is_no_remote_subkernel_with_the_specified_name()
        {
            var connector = CreateConnector();
            var action = async () => await connector.CreateProxyKernelAsync(remoteName: "non-existent");
            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task when_all_created_proxies_have_been_disposed_then_the_remote_process_is_killed()
        {
            var connector = CreateConnector();
            var rootProxyKernel = await connector.CreateRootProxyKernelAsync();
            var csharpProxyKernel = await connector.CreateProxyKernelAsync("csharp");
            var fsharpProxyKernel = await connector.CreateProxyKernelAsync("fsharp");

            using var _ = new AssertionScope();

            var processId = connector.ProcessId;
            processId.Should().NotBeNull();
            var process = Process.GetProcessById(processId.Value);
            process.HasExited.Should().BeFalse();

            csharpProxyKernel.Dispose();
            process.HasExited.Should().BeFalse();

            rootProxyKernel.Dispose();
            process.HasExited.Should().BeFalse();

            fsharpProxyKernel.Dispose();
            process.HasExited.Should().BeTrue();
        }
    }
}
