// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Interactive.Jupyter.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;
using Microsoft.Extensions.Hosting;
using NetMQ.Sockets;
using Pocket;
using Recipes;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Jupyter.Shell>;
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
        private readonly ReplyChannel _shellSender;
        private readonly PubSubChannel _ioPubSender;
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

            _shellSender = new ReplyChannel( new MessageSender(_shell, _signatureValidator));
            _ioPubSender = new PubSubChannel( new MessageSender(_ioPubSocket, _signatureValidator));

            _disposables = new CompositeDisposable
                           {
                               _shell,
                               _ioPubSocket,
                               _stdIn,
                               _control
                           };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var activity = Log.OnEnterAndExit();

            SetupDefaultMimeTypes();

            _shell.Bind(_shellAddress);
            _ioPubSocket.Bind(_ioPubAddress);
            _stdIn.Bind(_stdInAddress);
            _control.Bind(_controlAddress);
            var kernelIdentity = Guid.NewGuid().ToString();

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
                            _shellSender,
                            _ioPubSender,
                            request, 
                            kernelIdentity);

                        await _scheduler.Schedule(context);

                        await context.Done();

                        SetIdle(request);

                        break;
                }
            }

            void SetBusy(ZeroMQMessage request) => _ioPubSender.Publish(new Status(StatusValues.Busy), request, kernelIdentity);
            void SetIdle(ZeroMQMessage request) => _ioPubSender.Publish(new Status(StatusValues.Idle), request, kernelIdentity);
        }

        public static void SetupDefaultMimeTypes()
        {
            Microsoft.DotNet.Interactive.Formatting.Formatter<LaTeXString>.Register((laTeX, writer) =>
                {
                    writer.Write(laTeX.ToString());
                },
                "text/latex");

            Microsoft.DotNet.Interactive.Formatting.Formatter<MathString>.Register((math, writer) =>
                {
                    writer.Write(math.ToString());
                },
                "text/latex");

            Microsoft.DotNet.Interactive.Formatting.Formatter.SetPreferredMimeTypeFor(typeof(LaTeXString), "text/latex");
            Microsoft.DotNet.Interactive.Formatting.Formatter.SetPreferredMimeTypeFor(typeof(MathString), "text/latex");
            Microsoft.DotNet.Interactive.Formatting.Formatter.SetPreferredMimeTypeFor(typeof(string), Microsoft.DotNet.Interactive.Formatting.PlainTextFormatter.MimeType);
            Microsoft.DotNet.Interactive.Formatting.Formatter.SetDefaultMimeType(Microsoft.DotNet.Interactive.Formatting.HtmlFormatter.MimeType);
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
            _shellSender.Reply(kernelInfoReply, request);
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