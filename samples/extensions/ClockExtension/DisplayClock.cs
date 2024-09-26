using Microsoft.DotNet.Interactive.Commands;

namespace ClockExtension;

public class DisplayClock : KernelDirectiveCommand
{
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int Second { get; set; }
}