using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.NamedPipeConnector;

namespace WpfConnect
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private CompositeKernel _kernel;

        private const string NamedPipeName = "InteractiveWpf";

        private bool RunOnDispatcher { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _kernel = new CompositeKernel();

            AddDispatcherDirective(_kernel);

            CSharpKernel csharpKernel = RegisterCSharpKernel();

            SetUpNamedPipeKernelConnection();

            var _ = Task.Run(async () =>
            {
                //Load WPF app assembly 
                await csharpKernel.SendAsync(
                    new SubmitCode($"""
                                    #r "{typeof(App).Assembly.Location}"
                                    using {nameof(WpfConnect)};
                                    """));

                // Add the WPF app as a variable that can be accessed
                await csharpKernel.SetValueAsync("App", this, GetType());

                _kernel.AddConnectDirective(new ConnectNamedPipeDirective
                {
                    Name = NamedPipeName
                });
            });
        }

        private void SetUpNamedPipeKernelConnection()
        {
            var serverStream = new NamedPipeServerStream(
                NamedPipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);

            var sender = KernelCommandAndEventSender.FromNamedPipe(
                serverStream,
                new Uri("kernel://wpf-remote-control"));

            var receiver = KernelCommandAndEventReceiver.FromNamedPipe(serverStream);

            var host = _kernel.UseHost(sender, receiver, new Uri("kernel://my-wpf-app"));

            _kernel.RegisterForDisposal(host);
            _kernel.RegisterForDisposal(receiver);
            _kernel.RegisterForDisposal(serverStream);

            var _ = Task.Run(async () =>
            {
                // required as waiting connection on named pipe server will block
                await serverStream.WaitForConnectionAsync();
                await host.ConnectAsync();
            });
        }

        private void AddDispatcherDirective(Kernel kernel)
        {
            var dispatcherCommand = new KernelActionDirective("#!dispatcher")
            {
                Description = "Run the current code on the Dispatcher"
            };

            kernel.AddDirective(dispatcherCommand, (command, context) =>
            {
                RunOnDispatcher = true;
                return Task.CompletedTask;
            });
        }

        private CSharpKernel RegisterCSharpKernel()
        {
            var csharpKernel = new CSharpKernel()
                               .UseKernelHelpers()
                               .UseWho()
                               .UseValueSharing()
                               //This is added locally
                               .UseWpf();

            _kernel.Add(csharpKernel);

            csharpKernel.AddMiddleware(async (KernelCommand command, KernelInvocationContext context, KernelPipelineContinuation next) =>
            {
                if (RunOnDispatcher)
                {
                    await Dispatcher.InvokeAsync(async () => await next(command, context));
                    RunOnDispatcher = false;
                }
                else
                {
                    await next(command, context);
                }
            });

            return csharpKernel;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _kernel?.Dispose();
            base.OnExit(e);
        }
    }
}