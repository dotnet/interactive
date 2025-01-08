// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;
using Microsoft.Extensions.Hosting;
using NetMQ.Sockets;
using Pocket;
using Recipes;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Jupyter.Shell>;
using ZeroMQMessage = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class Shell : IHostedService
{
    private readonly Kernel _kernel;
    private readonly JupyterRequestContextScheduler _scheduler;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly RouterSocket _shell;
    private readonly PublisherSocket _ioPubSocket;
    private readonly string _shellAddress;
    private readonly string _ioPubAddress;
    private readonly CompositeDisposable _disposables;
    private readonly RequestReplyChannel _shellChannel;
    private readonly PubSubChannel _ioPubChannel;
    private readonly StdInChannel _stdInChannel;
    private readonly string _stdInAddress;
    private readonly string _controlAddress;
    private readonly RouterSocket _stdIn;
    private readonly RouterSocket _control;
    private readonly RequestReplyChannel _controlChannel;
    private string _kernelIdentity =  Guid.NewGuid().ToString();
    private CancellationToken _cancellationToken;
    private Task _shellChannelLoop;
    private Task _controlChannelLoop;

    public Shell(
        Kernel kernel,
        JupyterRequestContextScheduler scheduler,
        ConnectionInformation connectionInformation,
        IHostApplicationLifetime applicationLifetime)
    {
        if (connectionInformation is null)
        {
            throw new ArgumentNullException(nameof(connectionInformation));
        }

        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _applicationLifetime = applicationLifetime;

        _shellAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.ShellPort}";
        _ioPubAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.IOPubPort}";
        _stdInAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.StdinPort}";
        _controlAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.ControlPort}";

        var signatureAlgorithm = connectionInformation.SignatureScheme.Replace("-", string.Empty).ToUpperInvariant();
        var signatureValidator = new SignatureValidator(connectionInformation.Key, signatureAlgorithm);
        _shell = new RouterSocket();
        _ioPubSocket = new PublisherSocket();
        _stdIn = new RouterSocket();
        _control = new RouterSocket();

        _shellChannel = new RequestReplyChannel(new MessageSender(_shell, signatureValidator));
        _controlChannel = new RequestReplyChannel(new MessageSender(_control, signatureValidator));
        _ioPubChannel = new PubSubChannel(new MessageSender(_ioPubSocket, signatureValidator));
        _stdInChannel = new StdInChannel(new MessageSender(_stdIn, signatureValidator), new MessageReceiver(_stdIn));

        _disposables = new CompositeDisposable
        {
            _kernel,
            _shell,
            _ioPubSocket,
            _stdIn,
            _control
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;

        _shell.Bind(_shellAddress);
        _ioPubSocket.Bind(_ioPubAddress);
        _stdIn.Bind(_stdInAddress);
        _control.Bind(_controlAddress);

        _shellChannelLoop = Task.Factory.StartNew(ShellChannelLoop, creationOptions: TaskCreationOptions.LongRunning);

        _controlChannelLoop = Task.Factory.StartNew(ControlChannelLoop, creationOptions: TaskCreationOptions.LongRunning);

        return Task.CompletedTask;
    }

    private void ControlChannelLoop()
    {
        using var activity = Log.OnEnterAndExit();
        while (!_cancellationToken.IsCancellationRequested)
        {
            var request = _control.GetMessage();

            activity.Info("Received: {message}", request.ToJson());

            SetBusy(request);

            switch (request.Header.MessageType)
            {
                case JupyterMessageContentTypes.KernelShutdownRequest:
                    _controlChannel.Reply(new KernelShutdownReply(), request);
                    SetIdle(request);
                    _applicationLifetime.StopApplication();
                    break;
            }
        }
    }

    private async Task ShellChannelLoop()
    {
        using var activity = Log.OnEnterAndExit();
        while (!_cancellationToken.IsCancellationRequested)
        {
            var request = _shell.GetMessage();

            activity.Info("Received: {message}", request.ToJson());

            SetBusy(request);

            switch (request.Header.MessageType)
            {
                case JupyterMessageContentTypes.KernelInfoRequest:
                    _kernelIdentity = Encoding.Unicode.GetString(request.Identifiers[0].ToArray());
                    HandleKernelInfoRequest(request);
                    SetIdle(request);
                    break;

                case JupyterMessageContentTypes.KernelShutdownRequest:
                    _shellChannel.Reply(new KernelShutdownReply(), request);
                    SetIdle(request);
                    _applicationLifetime.StopApplication();
                    break;

                default:
                    var context = new JupyterRequestContext(
                        _shellChannel,
                        _ioPubChannel,
                        _stdInChannel,
                        request,
                        _kernelIdentity);

                    await _scheduler.Schedule(context);

                    await context.Done();

                    SetIdle(request);

                    break;
            }
        }
    }

    private void SetBusy(ZeroMQMessage request) => _ioPubChannel.Publish(new Status(StatusValues.Busy), request, _kernelIdentity);

    private void SetIdle(ZeroMQMessage request) => _ioPubChannel.Publish(new Status(StatusValues.Idle), request, _kernelIdentity);
        
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _disposables.Dispose();
        return Task.CompletedTask;
    }

    private void HandleKernelInfoRequest(ZeroMQMessage request)
    {
        var languageInfo = GetLanguageInfo();
        var kernelInfoReply = new KernelInfoReply(JupyterConstants.MESSAGE_PROTOCOL_VERSION, ".NET", "5.1.0", languageInfo);
        _shellChannel.Reply(kernelInfoReply, request);
    }

    private LanguageInfo GetLanguageInfo()
    {
        switch (_kernel)
        {
            case CompositeKernel composite:
                return GetLanguageInfo(composite.DefaultKernelName);
           
            default:
                return null;
        }
    }

    private LanguageInfo GetLanguageInfo(string kernelName) =>
        kernelName switch
        {
            "csharp" => new CSharpLanguageInfo(),
            "fsharp" => new FSharpLanguageInfo(),
            "powershell" => new PowerShellLanguageInfo(),
            _ =>  null
        };
}