﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;
using Microsoft.Playwright;
using Pocket;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Browser.PlaywrightKernelConnector>;
using CompositeDisposable = Pocket.CompositeDisposable;
using Disposable = System.Reactive.Disposables.Disposable;

namespace Microsoft.DotNet.Interactive.Browser;

public class PlaywrightKernelConnector : IKernelConnector
{
    private readonly RefCountDisposable _refCountDisposable;
    private readonly AsyncLazy<(IPage page, Subject<CommandOrEvent> commandsAndEvents)> _oneTimeBrowserSetup;

    public PlaywrightKernelConnector(bool launchBrowserHeadless = false)
    {
        var playwrightDisposable = new SerialDisposable();

        _refCountDisposable = new(playwrightDisposable);

        _oneTimeBrowserSetup = new AsyncLazy<(IPage page, Subject<CommandOrEvent> commandsAndEvents)>(async () =>
        {
            var playwright = await Playwright.Playwright.CreateAsync();

            var launch = await LaunchBrowserAsync(playwright, launchBrowserHeadless);

            var context = await launch.Browser.NewContextAsync();

            var page = await context.NewPageAsync();

            var commandsAndEvents = new Subject<CommandOrEvent>();

            string jsSource;

            var resourceName = "dotnet-interactive.js";
            var type = typeof(PlaywrightKernelConnector);
            using (var stream = type.Assembly.GetManifestResourceStream($"{type.Namespace}.{resourceName}"))
            using (var reader = new StreamReader(stream ?? throw new InvalidOperationException($"Resource \"{resourceName}\" not found"), Encoding.UTF8))
            {
                jsSource = await reader.ReadToEndAsync();
            }

            await page.ExposeFunctionAsync("publishCommandOrEvent", (JsonElement json) =>
            {
                var commandOrEvent = Serializer.DeserializeCommandOrEvent(json);

                commandsAndEvents.OnNext(commandOrEvent);
            });

            await page.EvaluateAsync(jsSource);

            await page.EvaluateAsync(@"dotnetInteractive.setup({hostName : ""browser""});");

            playwrightDisposable.Disposable = Disposable.Create(() => { playwright.Dispose(); });

            return (page, commandsAndEvents);
        });
    }

    public Task<Kernel> CreateKernelAsync(string kernelName)
    {
        var senderAndReceiver = new PlaywrightSenderAndReceiver(_oneTimeBrowserSetup);

        var proxy = new ProxyKernel(
            kernelName,
            senderAndReceiver,
            senderAndReceiver);

        proxy.KernelInfo.SupportedKernelCommands.Add(new KernelCommandInfo(nameof(SubmitCode)));

        switch (kernelName)
        {
            case "html":
                proxy.RegisterCommandHandler<RequestValueInfos>((request, context) =>
                {
                    context.Publish(new ValueInfosProduced(new KernelValueInfo[]
                    {
                        new(":root", typeof(ILocator))
                    }, request));
                    return Task.CompletedTask;
                });

                proxy.RegisterCommandHandler<RequestValue>(async (request, context) =>
                {
                    var (page, _) = await _oneTimeBrowserSetup.ValueAsync();

                    var selector = request.Name;

                    var html = request.MimeType switch
                    {
                        "image/jpeg" => await CaptureImageAsync(page, selector, ScreenshotType.Jpeg),
                        "image/png" => await CaptureImageAsync(page, selector, ScreenshotType.Png),
                        PlainTextFormatter.MimeType => await page.InnerTextAsync(selector),
                        _ => await page.InnerHTMLAsync(selector),
                    };

                    context.Publish(
                        new ValueProduced(
                            page.Locator(selector),
                            request.Name,
                            new FormattedValue("text/html", html),
                            request));
                });

                break;

            case "javascript":
                proxy.KernelInfo.SupportedKernelCommands.Add(new(nameof(SubmitCode)));
                proxy.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValue)));
                proxy.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValueInfos)));
                proxy.KernelInfo.SupportedKernelCommands.Add(new(nameof(SendValue)));
                proxy.UseValueSharing();
                break;
        }

        proxy.RegisterForDisposal(_refCountDisposable);
        proxy.RegisterForDisposal(_refCountDisposable.GetDisposable());

        return Task.FromResult<Kernel>(proxy);
    }

    private async Task<string> CaptureImageAsync(IPage page, string selector, ScreenshotType screenshotType)
    {
        var rawBytes = await page.Locator(selector).ScreenshotAsync(new LocatorScreenshotOptions
        {
            Type = screenshotType
        });

        return Convert.ToBase64String(rawBytes);
    }

    private static async Task<BrowserLaunch> LaunchBrowserAsync(IPlaywright playwright, bool headless)
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
                Headless = headless
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
        private bool _remoteKernelIsLoaded;
        private readonly AsyncLazy<(IPage page, Subject<CommandOrEvent> commandsAndEvents)> _lazy;
        private readonly Subject<CommandOrEvent> _commandsAndEvents = new();

        public PlaywrightSenderAndReceiver(AsyncLazy<(IPage page, Subject<CommandOrEvent> commandsAndEvents)> lazy)
        {
            _lazy = lazy;
            RemoteHostUri = new("kernel://browser");
        }

        public async Task SendAsync(KernelCommand command, CancellationToken cancellationToken)
        {
            await Task.Yield();

            await EnsureRemoteKernelIsLoadedAsync();

            var (page, _) = await _lazy.ValueAsync();

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

            var (_, subject) = await _lazy.ValueAsync();

            var serialDisposable = new SerialDisposable();

            serialDisposable.Disposable = subject.Subscribe(
                _commandsAndEvents.OnNext,
                onError: exception =>
                {
                    _commandsAndEvents.OnError(exception);
                    serialDisposable.Dispose();
                },
                onCompleted: () =>
                {
                    _commandsAndEvents.OnCompleted();
                    serialDisposable.Dispose();
                });

            _remoteKernelIsLoaded = true;
        }

        public Uri RemoteHostUri { get; }
    }
}