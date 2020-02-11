using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;

namespace RxClockExtension
{
    public class RxClockKernelExtension : IKernelExtension
    {
        public async Task OnLoadAsync(IKernel kernel)
        {
            Formatter<DateTime>.Register((d, writer) => { writer.Write(d.DrawSvgClock()); }, "text/html");

            Formatter<DateTimeOffset>.Register((d, writer) => { writer.Write(d.DrawSvgClock()); }, "text/html");

            Formatter.Register(typeof(IObservable<DateTime>), (o, writer) =>
            {
                var ts = o as IObservable<DateTime>;

                var firstAsync = Task.Run(async () => await ts.FirstAsync()).Result;

                writer.Write(firstAsync.DrawSvgClock());

                if (KernelInvocationContext.Current is {} context)
                {
                    // Task.Run(async () =>
                    // {
                    //
                    //     var d =await context.DisplayAsync();
                    //
                    //
                    // Observable
                    //     .Range(1, 10)
                    //     .Select(i => t.)
                    //     .Delay(TimeSpan.FromSeconds())
                    //     .Take(10)
                    //     .ObserveOn(Scheduler.CurrentThread)
                    //     .Subscribe(_ => 
                    //     {
                    //         d.Update(DateTime.Now);
                    //     });
                    //
                    // } );
                    //
                }
            }, "text/html");

            await KernelInvocationContext.Current.DisplayAsync("Now you can format System.DateTime and System.DateTimeOffset");
        }
    }
}