// See https://aka.ms/new-console-template for more information
using TspServer;

var server = new TcpServer();
await server.StartAsync();

Console.ReadKey();

server.Dispose();

Console.ReadKey();
