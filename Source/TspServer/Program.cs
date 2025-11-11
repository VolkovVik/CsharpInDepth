using TspServer;

CancellationTokenSource _cancellationTokenSource = new();

try
{
    Console.CancelKeyPress += OnCancelKeyPress;
    Console.WriteLine("Application started...");
    Console.WriteLine("Press Ctrl+C to exit");

    await RunApplicationAsync(_cancellationTokenSource.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("The operation was cancelled");
}
finally
{
    _cancellationTokenSource.Dispose();
    Console.WriteLine("Application stopped");
}

void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    Console.WriteLine($"Key press: {e.SpecialKey}");

    if (e.SpecialKey != ConsoleSpecialKey.ControlC)
        return;

    e.Cancel = true;
    _cancellationTokenSource.Cancel();
}

static async Task RunApplicationAsync(CancellationToken cancellationToken)
{
    using var store = new SimpleStore();
    using var server = new TcpServer(store);

    await server.StartAsync(cancellationToken: cancellationToken);

    while (!cancellationToken.IsCancellationRequested)
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
