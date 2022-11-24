using BenchmarkDotNet.Running;

namespace PerfTests;

class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}