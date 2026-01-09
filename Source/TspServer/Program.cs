using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TspServer;

CancellationTokenSource cancellationTokenSource = new();

try
{
    Console.CancelKeyPress += OnCancelKeyPress;
    Console.WriteLine("Application started...");
    Console.WriteLine("Press Ctrl+C to exit");

    Console.WriteLine("OpenTelemetry starting...");
    var resource = BuildOpenTelemetryResource();
    using var tracerProvider = ConfigureTracing(resource);
    using var meterProvider = ConfigureMetrics(resource);
    Console.WriteLine("OpenTelemetry started");

    await RunApplicationAsync(cancellationTokenSource.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("The operation was cancelled");
}
finally
{
    cancellationTokenSource.Dispose();

    Console.WriteLine("Application stopped");
}

return;

void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    Console.WriteLine($"Key press: {e.SpecialKey}");

    if (e.SpecialKey != ConsoleSpecialKey.ControlC)
        return;

    e.Cancel = true;
    // ReSharper disable once AccessToDisposedClosure
    cancellationTokenSource.Cancel();
}

async Task RunApplicationAsync(CancellationToken cancellationToken)
{
    using var store = new SimpleStore();
    using var server = new TcpServer(store);

    await server.StartAsync(cancellationToken: cancellationToken);

    while (!cancellationToken.IsCancellationRequested)
    // ReSharper disable once RemoveRedundantBraces
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("The task was cancelled");
            break;
        }
    }
}

// Создаем Resource для идентификации сервиса
ResourceBuilder BuildOpenTelemetryResource() =>
    ResourceBuilder.CreateDefault()
        .AddService(
            serviceName: OpenTelemetryConstants.ServiceName,
            serviceVersion: OpenTelemetryConstants.ServiceVersion)
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment"] = "development",
            ["application"] = "console-app"
        });

// Настройка трассировки (Traces)
TracerProvider ConfigureTracing(ResourceBuilder resource) =>
    Sdk.CreateTracerProviderBuilder()
        .SetResourceBuilder(resource)
        .AddSource(OpenTelemetryConstants.ServiceName)
        .AddConsoleExporter(options =>
        {
            options.Targets = ConsoleExporterOutputTargets.Console;
        })
        .Build();

// Настройка метрик (Metrics)
MeterProvider ConfigureMetrics(ResourceBuilder resource) =>
    Sdk.CreateMeterProviderBuilder()
        .SetResourceBuilder(resource)
        .AddMeter(OpenTelemetryConstants.ServiceName)
        .AddConsoleExporter((exporterOptions, metricReaderOptions) =>
        {
            metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 10000;
        })
        .Build();
