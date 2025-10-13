using System.Buffers;
using System.Collections.Concurrent;
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

    private readonly ConcurrentDictionary<Socket, SemaphoreSlim> _semaphores = new();

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
                var clientSocket = await _serverSocket!.AcceptAsync();

                var clientSemaphore = new SemaphoreSlim(1, 1);
                _semaphores.TryAdd(clientSocket, clientSemaphore);

                _ = Task.Run(async () => await ProcessClientAsync(clientSocket, clientSemaphore));
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

    private async Task ProcessClientAsync(Socket socket, SemaphoreSlim semaphore)
    {
        Console.WriteLine($"TCP client {socket.RemoteEndPoint} connected");

        var arrayPool = ArrayPool<byte>.Shared;
        var buffer = arrayPool.Rent(4096);
        var memory = new Memory<byte>(buffer);

        try
        {
            while (socket.Connected)
            {
                var result = await ProcessClientInternalAsync(socket, semaphore, memory);
                if (!result)
                    break;
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"TCP client socket error {socket.RemoteEndPoint}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TCP client error {socket.RemoteEndPoint}: {ex.Message}");
        }
        finally
        {
            arrayPool.Return(buffer);

            CloseSemaphore(socket, semaphore);

            CloseSocket(socket);
        }
    }

    private static async Task<bool> ProcessClientInternalAsync(Socket socket, SemaphoreSlim semaphore, Memory<byte> memory)
    {
        var isAcquired = false;

        try
        {
            isAcquired = await semaphore.WaitAsync(TimeSpan.FromMilliseconds(500));
            if (!isAcquired)
            {
                Console.WriteLine($"TCP client {socket.RemoteEndPoint}. Semaphore capture timeout");
                return true;
            }

            var bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
            if (bytesRead < 1)
                return false;

            var span = memory[..bytesRead].Span;
            var command = CommandParser<byte>.Parse(span, (byte)' ');

            Console.WriteLine($"TCP client {socket.RemoteEndPoint} send {command.ToString()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TCP client error {socket.RemoteEndPoint}: {ex.Message}");
            return false;
        }
        finally
        {
            if (isAcquired)
                semaphore.Release();
        }
        return true;
    }


    private void CloseSemaphore(Socket socket, SemaphoreSlim semaphore)
    {
        if (socket is null)
            return;

        try
        {
            _semaphores.TryRemove(socket, out _);
            semaphore?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TCP socket {socket.RemoteEndPoint}. Close semaphore error: {ex.Message}");
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

            foreach ((var socket, var semaphore) in _semaphores)
            {
                socket?.Shutdown(SocketShutdown.Both);
                socket?.Close();
                socket?.Dispose();

                semaphore?.Dispose();
            }
            _semaphores.Clear();

            Console.WriteLine("TCP server stopped");
        }

        _isDisposed = true;
    }

    ~TcpServer() =>
       Dispose(false);
}
