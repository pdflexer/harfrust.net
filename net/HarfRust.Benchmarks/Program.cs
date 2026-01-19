using BenchmarkDotNet.Running;

namespace HarfRust.Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
