using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using TspServer;

BenchmarkRunner.Run<CommandParserTests>();

// dotnet build -c Release
// dotnet run -c Release

[MemoryDiagnoser]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1050:Declare types in namespaces", Justification = "<Pending>")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Bug", "S3903:Types should be defined in named namespaces", Justification = "<Pending>")]
public class CommandParserTests
{
    [Benchmark]
    public void WithSpanBuilder()
    {
        var data = Encoding.UTF8.GetBytes("SET user:1 data");
        var span = new ReadOnlySpan<byte>(data);

        CommandParser<byte>.Parse(span, (byte)' ');
    }
}
