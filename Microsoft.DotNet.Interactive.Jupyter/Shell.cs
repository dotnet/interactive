// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;
using Microsoft.Extensions.Hosting;
using NetMQ.Sockets;
using Pocket;
using Recipes;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Jupyter.Shell>;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;
using ZeroMQMessage = Microsoft.DotNet.Interactive.Jupyter.ZMQ.Message;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class Shell : IHostedService
    {
        private readonly IKernel _kernel;
        private readonly ICommandScheduler<JupyterRequestContext> _scheduler;
        private readonly RouterSocket _shell;
        private readonly PublisherSocket _ioPubSocket;
        private readonly string _shellAddress;
        private readonly string _ioPubAddress;
        private readonly SignatureValidator _signatureValidator;
        private readonly CompositeDisposable _disposables;
        private readonly ReplyChannel _shellChannel;
        private readonly PubSubChannel _ioPubChannel;
        private readonly StdInChannel _stdInChannel;
        private readonly string _stdInAddress;
        private readonly string _controlAddress;
        private readonly RouterSocket _stdIn;
        private readonly RouterSocket _control;

        public Shell(
            IKernel kernel,
            ICommandScheduler<JupyterRequestContext> scheduler,
            ConnectionInformation connectionInformation)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

            _shellAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.ShellPort}";
            _ioPubAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.IOPubPort}";
            _stdInAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.StdinPort}";
            _controlAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.ControlPort}";

            var signatureAlgorithm = connectionInformation.SignatureScheme.Replace("-", string.Empty).ToUpperInvariant();
            _signatureValidator = new SignatureValidator(connectionInformation.Key, signatureAlgorithm);
            _shell = new RouterSocket();
            _ioPubSocket = new PublisherSocket();
            _stdIn = new RouterSocket();
            _control = new RouterSocket();

            _shellChannel = new ReplyChannel(new MessageSender(_shell, _signatureValidator));
            _ioPubChannel = new PubSubChannel(new MessageSender(_ioPubSocket, _signatureValidator));
            _stdInChannel = new StdInChannel(new MessageSender(_stdIn, _signatureValidator), new MessageReceiver(_stdIn));

            _disposables = new CompositeDisposable
                           {
                               _shell,
                               _ioPubSocket,
                               _stdIn,
                               _control
                           };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            SetupDefaultMimeTypes();

            _shell.Bind(_shellAddress);
            _ioPubSocket.Bind(_ioPubAddress);
            _stdIn.Bind(_stdInAddress);
            _control.Bind(_controlAddress);
            var kernelIdentity = Guid.NewGuid().ToString();
            Task.Run(async () =>
            {
                using var activity = Log.OnEnterAndExit();
                while (!cancellationToken.IsCancellationRequested)
                {
                    var request = _shell.GetMessage();

                    activity.Info("Received: {message}", request.ToJson());

                    SetBusy(request);

                    switch (request.Header.MessageType)
                    {
                        case JupyterMessageContentTypes.KernelInfoRequest:
                            kernelIdentity = Encoding.Unicode.GetString(request.Identifiers[0].ToArray());
                            HandleKernelInfoRequest(request);
                            SetIdle(request);
                            break;

                        case JupyterMessageContentTypes.KernelShutdownRequest:
                            SetIdle(request);
                            break;

                        default:
                            var context = new JupyterRequestContext(
                                _shellChannel,
                                _ioPubChannel,
                                _stdInChannel,
                                request,
                                kernelIdentity);

                            await _scheduler.Schedule(context);

                            await context.Done();

                            SetIdle(request);

                            break;
                    }
                }
            }, cancellationToken);

            void SetBusy(ZeroMQMessage request) => _ioPubChannel.Publish(new Status(StatusValues.Busy), request, kernelIdentity);
            void SetIdle(ZeroMQMessage request) => _ioPubChannel.Publish(new Status(StatusValues.Idle), request, kernelIdentity);

            return Task.CompletedTask;
        }

        public static void SetupDefaultMimeTypes()
        {
            Formatter<LaTeXString>.Register((laTeX, writer) => writer.Write(laTeX.ToString()), "text/latex");

            Formatter<MathString>.Register((math, writer) => writer.Write(math.ToString()), "text/latex");

            Formatter.SetPreferredMimeTypeFor(typeof(LaTeXString), "text/latex");
            Formatter.SetPreferredMimeTypeFor(typeof(MathString), "text/latex");
            
            Formatter.SetPreferredMimeTypeFor(typeof(string), HtmlFormatter.MimeType);
            
            Formatter.SetDefaultMimeType(HtmlFormatter.MimeType);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _disposables.Dispose();
            return Task.CompletedTask;
        }

        private void HandleKernelInfoRequest(ZeroMQMessage request)
        {
            var languageInfo = GetLanguageInfo();
            var kernelInfoReply = new KernelInfoReply(Constants.MESSAGE_PROTOCOL_VERSION, ".NET", "5.1.0", languageInfo);
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
}