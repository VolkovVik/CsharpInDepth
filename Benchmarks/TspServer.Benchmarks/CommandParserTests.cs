using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using TspServer;

BenchmarkRunner.Run<CommandParserTests>();
BenchmarkRunner.Run<SerializationBenchmarks>();

// dotnet build -c Release
// dotnet run -c Release

[MemoryDiagnoser]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1050:Declare types in namespaces", Justification = "<Pending>")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Bug", "S3903:Types should be defined in named namespaces", Justification = "<Pending>")]
#pragma warning disable CA1050
public class CommandParserTests
#pragma warning restore CA1050
{
    [Benchmark]
    public void WithSpanBuilder()
    {
        var data = "SET user:1 data"u8.ToArray();
        var span = new ReadOnlySpan<byte>(data);

        CommandParser<byte>.Parse(span, (byte)' ');
    }
}
