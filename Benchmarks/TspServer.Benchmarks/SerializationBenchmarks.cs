using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using TspServer;

[MemoryDiagnoser]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1050:Declare types in namespaces", Justification = "<Pending>")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Bug", "S3903:Types should be defined in named namespaces", Justification = "<Pending>")]
public class SerializationBenchmarks
{
    private readonly UserProfile _profile = new() { Id = 1, Username = "John Doe" };

    private readonly JsonSerializerOptions _stjOptions =
        new(JsonSerializerDefaults.General);

    private readonly JsonSerializerSettings _newtonsoftSettings =
        new();

    [Benchmark(Baseline = true)]
    public byte[] NewtonsoftJson()
    {
        var json = JsonConvert.SerializeObject(_profile, _newtonsoftSettings);
        return Encoding.UTF8.GetBytes(json);
    }

    [Benchmark]
    public byte[] SystemTextJson() =>
        System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(_profile, _stjOptions);

    [Benchmark]
    public void SourceGenerator()
    {
        using var ms = new MemoryStream();
        _profile.SerializeToBinary(ms);
    }
}
