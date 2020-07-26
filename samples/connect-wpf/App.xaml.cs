using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Server;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //TODO: Dispose
        private CompositeKernel _Kernel;
        
        private const string NamedPipeName = "InteractiveWpf";
        private bool _RunOnDispatcher;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _Kernel = new CompositeKernel();

            var csharpKernel = new CSharpKernel()
                .UseDefaultFormatting()
                //WPF Formatters here
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseWho()
                .UseDotNetVariableSharing();

            _Kernel.Add(csharpKernel);
            _Kernel.UseLog();

            var dispatcherCommand = new Command("#!dispatcher", "Enable or disable running code on the Dispatcher")
            {
                new Option<bool>("--enabled", getDefaultValue:() => true)
            };
            dispatcherCommand.Handler = CommandHandler.Create<KernelInvocationContext, bool>(OnDispatcher);
            _Kernel.AddDirective(dispatcherCommand);

            csharpKernel.AddMiddleware(async (KernelCommand command, KernelInvocationContext context, KernelPipelineContinuation next) =>
            {
                if (_RunOnDispatcher)
                {
                    await Dispatcher.InvokeAsync(async () => await next(command, context));
                }
                else
                {
                    await next(command, context);
                }
            });

            _ = Task.Run(async () =>
              {
                  await csharpKernel.SendAsync(new SubmitCode(@$"#r ""{typeof(App).Assembly.Location}""
using WpfApp1;"));
                  await csharpKernel.SetVariableAsync("App", this);

                  _Kernel.UseNamedPipeKernelServer(NamedPipeName, new DirectoryInfo("."));
              });
        }

        private void OnDispatcher(KernelInvocationContext context, bool enabled)
        {
            _RunOnDispatcher = enabled;
        }
    }
}
