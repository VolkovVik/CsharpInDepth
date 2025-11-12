using System.Collections.Concurrent;
using NBomber.CSharp;
using TspServer.LoadTests;

// dotnet build -c Release
// dotnet run -c Release

const int MaxRate = 100;

var _messageId = 0;
var clientPool = new ConcurrentBag<SimpleTcpClient>();

Console.WriteLine("Run load testing");

var scenario = Scenario.Create("tcp_scenario", async context =>
{
    await Step.Run("set value step", context, async () =>
    {
        if (!clientPool.TryTake(out var client))
        {
            client = new SimpleTcpClient();
            await client.ConnectAsync();
        }

        try
        {
            var messageId = Interlocked.Increment(ref _messageId);
            var result = await client.SetAsync($"key{messageId}", $"value{messageId}");
            clientPool.Add(client);
            return result.StartsWith("OK", StringComparison.OrdinalIgnoreCase) ? Response.Ok() : Response.Fail();
        }
        catch (Exception ex)
        {
            client.Dispose();
            context.Logger.Error("Exception: {@Exception}", ex.Message);
            return Response.Fail(message: ex.Message);
        }
    });
    return Response.Ok();
})
.WithInit(async context =>
{
    for (var i = 0; i < MaxRate; i++)
    {
        var client = new SimpleTcpClient();
        await client.ConnectAsync();

        clientPool.Add(client);
    }
})
.WithClean(context =>
{
    foreach (var client in clientPool)
        client.Dispose();
    clientPool.Clear();

    return Task.CompletedTask;
})
.WithWarmUpDuration(TimeSpan.FromSeconds(10))
.WithLoadSimulations(
    //Simulation.RampingInject(rate: MaxRate, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
    Simulation.Inject(rate: MaxRate, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
//Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
);

NBomberRunner
   .RegisterScenarios(scenario)
   .Run();

