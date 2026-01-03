using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TspServer;

TracerProvider? tracerProvider = null;
MeterProvider? meterProvider = null;

CancellationTokenSource cancellationTokenSource = new();

try
{
    Console.CancelKeyPress += OnCancelKeyPress;
    Console.WriteLine("Application started...");
    Console.WriteLine("Press Ctrl+C to exit");

    await RunApplicationAsync(cancellationTokenSource.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("The operation was cancelled");
}
finally
{
    tracerProvider?.Dispose();
    meterProvider?.Dispose();

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
    SetupOpenTelemetry();

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

void SetupOpenTelemetry()
{
    Console.WriteLine("OpenTelemetry starting...");
    // Создаем Resource для идентификации сервиса
    var resourceBuilder = ResourceBuilder.CreateDefault()
        .AddService(
            serviceName: OpenTelemetryConstants.ServiceName,
            serviceVersion: OpenTelemetryConstants.ServiceVersion)
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment"] = "development",
            ["application"] = "console-app"
        });

    // Настройка трассировки (Traces)
    tracerProvider = Sdk.CreateTracerProviderBuilder()
        .SetResourceBuilder(resourceBuilder)
        .AddSource(OpenTelemetryConstants.ServiceName)
        .AddConsoleExporter(options =>
        {
            options.Targets = ConsoleExporterOutputTargets.Console;
        })
        .Build();

    // Настройка метрик (Metrics)
    meterProvider = Sdk.CreateMeterProviderBuilder()
        .SetResourceBuilder(resourceBuilder)
        .AddMeter(OpenTelemetryConstants.ServiceName)
        .AddConsoleExporter((exporterOptions, metricReaderOptions) =>
        {
            metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 10000;
        })
        .Build();

    Console.WriteLine("OpenTelemetry started");
}
