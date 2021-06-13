using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Server;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace WpfConnect
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private CompositeKernel _Kernel;
        
        private const string NamedPipeName = "InteractiveWpf";

        private bool RunOnDispatcher { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _Kernel = new CompositeKernel();
            _Kernel.UseLogMagicCommand();

            AddDispatcherCommand(_Kernel);

            CSharpKernel csharpKernel = RegisterCSharpKernel();

            _ = Task.Run(async () =>
              {
                  //Load WPF app assembly 
                  await csharpKernel.SendAsync(new SubmitCode(@$"#r ""{typeof(App).Assembly.Location}""
using {nameof(WpfConnect)};"));
                  //Add the WPF app as a variable that can be accessed
                  await csharpKernel.SetVariableAsync("App", this);

                  //Start named pipe
                  _Kernel.UseNamedPipeKernelServer(NamedPipeName, new DirectoryInfo("."));
              });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _Kernel?.Dispose();
            base.OnExit(e);
        }

        private void AddDispatcherCommand(Kernel kernel)
        {
            var dispatcherCommand = new Command("#!dispatcher", "Enable or disable running code on the Dispatcher")
            {
                new Option<bool>("--enabled", getDefaultValue:() => true)
            };
            dispatcherCommand.Handler = CommandHandler.Create<bool>(enabled =>
            {
                RunOnDispatcher = enabled;
            });
            kernel.AddDirective(dispatcherCommand);
        }

        private CSharpKernel RegisterCSharpKernel()
        {
            var csharpKernel = new CSharpKernel()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseWho()
                .UseDotNetVariableSharing()
                //This is added locally
                .UseWpf();

            _Kernel.Add(csharpKernel);

            csharpKernel.AddMiddleware(async (KernelCommand command, KernelInvocationContext context, KernelPipelineContinuation next) =>
            {
                if (RunOnDispatcher)
                {
                    await Dispatcher.InvokeAsync(async () => await next(command, context));
                }
                else
                {
                    await next(command, context);
                }
            });

            return csharpKernel;
        }
    }
}
