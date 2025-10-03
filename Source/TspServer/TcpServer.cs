using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace TspServer;

/// <summary>
/// TCP server
/// </summary>
public class TcpServer : IDisposable
{
    private bool _isRunning;
    private bool _isDisposed;
    private Socket? _serverSocket;

    public async Task StartAsync(string ipAddress = "127.0.0.1", int port = 8080)
    {
        var ip = string.IsNullOrWhiteSpace(ipAddress) ? IPAddress.Any : IPAddress.Parse(ipAddress);
        var endPoint = new IPEndPoint(ip, port);

        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _serverSocket.Bind(endPoint);
        _serverSocket.Listen(100);

        _isRunning = true;

        Console.WriteLine($"TCP server {ip}:{port} started");

        await AcceptConnectionsAsync();
    }

    private async Task AcceptConnectionsAsync()
    {
        while (_serverSocket != null && _isRunning)
        {
            try
            {
                var clientSocket = await _serverSocket.AcceptAsync();
                _ = Task.Run(async () => await ProcessClientAsync(clientSocket));
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"TCP server socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TCP server error: {ex.Message}");
            }
        }
    }

    private static async Task ProcessClientAsync(Socket clientSocket)
    {
        Console.WriteLine($"TCP client {clientSocket.RemoteEndPoint} connected");

        var arrayPool = ArrayPool<byte>.Shared;
        var buffer = arrayPool.Rent(4096);
        var memory = new Memory<byte>(buffer);

        try
        {
            while (clientSocket.Connected)
            {
                var bytesRead = await clientSocket.ReceiveAsync(memory, SocketFlags.None);
                if (bytesRead < 1)
                    break;

                var readOnlySpan = memory[..bytesRead].Span;
                var command = CommandParser<byte>.Parse(readOnlySpan, (byte)' ');
                Console.WriteLine($"TCP client {clientSocket.RemoteEndPoint} send {command.ToString()}");
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"TCP client socket error {clientSocket.RemoteEndPoint}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TCP client error {clientSocket.RemoteEndPoint}: {ex.Message}");
        }
        finally
        {
            arrayPool.Return(buffer);

            CloseSocket(clientSocket);
        }
    }

    private static void CloseSocket(Socket socket)
    {
        if (socket is null)
            return;

        var endPoint = socket.RemoteEndPoint?.ToString();
        Console.WriteLine($"TCP socket {endPoint} closing...");

        try
        {
            if (socket.Connected)
                socket.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"TCP socket {endPoint} error: {ex.Message}");
        }
        finally
        {
            socket.Close();
            socket.Dispose();

            Console.WriteLine($"TCP socket {endPoint} closed");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            _isRunning = false;

            Console.WriteLine("TCP server stopping...");

            _serverSocket?.Close();
            _serverSocket?.Dispose();
            _serverSocket = null;

            Console.WriteLine("TCP server stopped");
        }

        _isDisposed = true;
    }

    ~TcpServer() =>
       Dispose(false);
}
