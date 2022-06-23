// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.Playwright;

namespace Microsoft.DotNet.Interactive.Browser;

public class PlaywrightKernelConnector : IKernelConnector
{
    public Task<Kernel> CreateKernelAsync(string kernelName)
    {
        var disposables = new CompositeDisposable();

        var browserChannel = "msedge";

        var _page = new AsyncLazy<IPage>(async () =>
        {
            var playwright = await Playwright.Playwright.CreateAsync();

            var options = new BrowserTypeLaunchOptions
            {
                Channel = browserChannel
            };

            if (Debugger.IsAttached)
            {
                options.Headless = false;
            }

            var browser = await playwright.Chromium.LaunchAsync(options);

            var context = await browser.NewContextAsync();

            var page = await context.NewPageAsync();

            disposables.Add(playwright);

            return page;
        });

        var senderAndReceiver = new PlaywrightSenderAndReceiver(_page, browserChannel);

        var proxy = new ProxyKernel(
            kernelName,
            senderAndReceiver,
            senderAndReceiver);

        proxy.RegisterForDisposal(disposables);

        return Task.FromResult<Kernel>(proxy);
    }

    private class PlaywrightSenderAndReceiver : IKernelCommandAndEventSender, IKernelCommandAndEventReceiver
    {
        private readonly AsyncLazy<IPage> _page;
        private bool _remoteKernelIsLoaded;
        private readonly Subject<CommandOrEvent> _commandsAndEvents = new();

        public PlaywrightSenderAndReceiver(AsyncLazy<IPage> page, string browserChannel)
        {
            _page = page;
            RemoteHostUri = new($"kernel://{browserChannel}/");
        }

        public async Task SendAsync(KernelCommand command, CancellationToken cancellationToken)
        {
            await Task.Yield();

            await EnsureRemoteKernelIsLoadedAsync();

            var page = await _page.ValueAsync();

            var commandJson = KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(command));

            await page.EvaluateAsync(@"(commandJson) => {
    const command = JSON.parse(commandJson);
    sendKernelCommand(command);
}", commandJson);
        }

        public Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public IDisposable Subscribe(IObserver<CommandOrEvent> observer) =>
            _commandsAndEvents.Subscribe(observer);

        private async Task EnsureRemoteKernelIsLoadedAsync()
        {
            if (_remoteKernelIsLoaded)  
            {
                return;
            }

            string jsSource;

            var resourceName = "dotnet-interactive.js";
            var type = typeof(PlaywrightKernelConnector);
            using (var stream = type.Assembly.GetManifestResourceStream($"{type.Namespace}.{resourceName}"))
            using (var reader = new StreamReader(stream ?? throw new InvalidOperationException($"Resource \"{resourceName}\" not found"), Encoding.UTF8))
            {
                jsSource = await reader.ReadToEndAsync();
            }

            var page = await _page.ValueAsync();
            await page.ExposeFunctionAsync("publishCommandOrEvent", (JsonElement  json) =>
            {
                var commandOrEvent = Serializer.DeserializeCommandOrEvent(json);

                _commandsAndEvents.OnNext(commandOrEvent);
            });

            await page.EvaluateAsync(jsSource);

            await page.EvaluateAsync(@"dotnetInteractive.setup();");

            _remoteKernelIsLoaded = true;
        }

        public Uri RemoteHostUri { get; }
    }
}