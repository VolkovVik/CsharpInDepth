using TspServer;

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

static async Task RunApplicationAsync(CancellationToken cancellationToken)
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
