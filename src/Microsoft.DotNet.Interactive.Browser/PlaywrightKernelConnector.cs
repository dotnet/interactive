// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.Playwright;
using Pocket;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Browser.PlaywrightKernelConnector>;
using CompositeDisposable = Pocket.CompositeDisposable;

namespace Microsoft.DotNet.Interactive.Browser;

public class PlaywrightKernelConnector : IKernelConnector
{
    public Task<Kernel> CreateKernelAsync(string kernelName)
    {
        var disposables = new CompositeDisposable();

        string? browserChannel = null;

        var _page = new AsyncLazy<IPage>(async () =>
        {
            var playwright = await Playwright.Playwright.CreateAsync();

            var launch = await LaunchBrowserAsync(playwright);

            browserChannel = launch.Options.Channel;

            var context = await launch.Browser.NewContextAsync();

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

    private static async Task<BrowserLaunch> LaunchBrowserAsync(IPlaywright playwright)
    {
        return
            await TryLaunch("msedge") ??
            await TryLaunch("chrome") ??
            await TryLaunch("chromium", true) ??
            throw new PlaywrightException("Unable to launch or acquire any of the configured browsers.");

        async Task<BrowserLaunch?> TryLaunch(string channel, bool acquire = false)
        {
            using var activity = Log.OnEnterAndConfirmOnExit();

            if (acquire)
            {
                Console.WriteLine($"Attempting to install headless browser: {channel}");

                var stdOut = new StringBuilder();
                var stdErr = new StringBuilder();

                using var console = ConsoleOutput.Subscribe(c => new CompositeDisposable
                {
                    c.Out.Subscribe(s => stdOut.Append(s)),
                    c.Error.Subscribe(s => stdErr.Append(s))
                });

                var exitCode = Program.Main(new[] { "install", channel });
                if (exitCode != 0)
                {
                    var message = $"Playwright browser acquisition failed with exit code {exitCode}.\n{stdOut}\n{stdErr}";
                    
                    activity.Fail(message: message);
                    
                    throw new PlaywrightException(message);
                }
            }

            var options = new BrowserTypeLaunchOptions
            {
                Channel = channel,
                Headless = !Debugger.IsAttached
            };

            try
            {
                activity.Info($"Launching headless browser {channel}");
                var browser = await playwright.Chromium.LaunchAsync(options);
                return new(browser, options);
            }
            catch (Exception ex)
            {
                activity.Fail(ex, $"Exception while launching headless browser {channel}");
            }

            return null;
        }
    }

    private record BrowserLaunch(IBrowser Browser, BrowserTypeLaunchOptions Options);

    private class PlaywrightSenderAndReceiver : IKernelCommandAndEventSender, IKernelCommandAndEventReceiver
    {
        private readonly AsyncLazy<IPage> _page;
        private bool _remoteKernelIsLoaded;
        private readonly Subject<CommandOrEvent> _commandsAndEvents = new();

        public PlaywrightSenderAndReceiver(AsyncLazy<IPage> page, string? browserChannel = null)
        {
            _page = page;
            RemoteHostUri = new($"kernel://{browserChannel?? "browser"}/");
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
            await page.ExposeFunctionAsync("publishCommandOrEvent", (JsonElement json) =>
            {
                var commandOrEvent = Serializer.DeserializeCommandOrEvent(json);

                _commandsAndEvents.OnNext(commandOrEvent);
            });

            await page.EvaluateAsync(jsSource);

            await page.EvaluateAsync($@"dotnetInteractive.setup({{hostName : ""{RemoteHostUri.Host}""}});");

            _remoteKernelIsLoaded = true;
        }

        public Uri RemoteHostUri { get; }
    }
}