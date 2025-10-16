using TspServer;

using var simpleStore = new SimpleStore();
using var server = new TcpServer(simpleStore);

await server.StartAsync();

Console.ReadKey();
