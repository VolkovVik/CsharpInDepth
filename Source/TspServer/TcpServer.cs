using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace TspServer;

/// <summary>
/// TCP server
/// </summary>
public sealed class TcpServer(SimpleStore simpleStore) : IDisposable
{
    private bool _isDisposed;
    private Socket? _serverSocket;

    private const int MaxBufferSize = 8192;
    private const int MaxCommandSize = 4096;

    private readonly SemaphoreSlim _serverSemaphore = new(2, 2);
    private readonly ConcurrentDictionary<Socket, SemaphoreSlim> _semaphores = new();

    private static readonly Counter<long> OperationsCounter =
        OpenTelemetryConstants.MyMeter.CreateCounter<long>("operations.count");

    private static readonly Histogram<double> OperationsTimeHistogram =
        OpenTelemetryConstants.MyMeter.CreateHistogram<double>("operations.time");

    public async Task StartAsync(string ipAddress = "127.0.0.1", int port = 8080, CancellationToken cancellationToken = default)
    {
        var ip = string.IsNullOrWhiteSpace(ipAddress) ? IPAddress.Any : IPAddress.Parse(ipAddress);
        var endPoint = new IPEndPoint(ip, port);

        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _serverSocket.Bind(endPoint);
        _serverSocket.Listen(100);

        Console.WriteLine($"TCP server {ip}:{port} started");

        await AcceptConnectionsAsync(cancellationToken);
    }

    private async Task AcceptConnectionsAsync(CancellationToken cancellationToken = default)
    {
        while (_serverSocket != null && !cancellationToken.IsCancellationRequested)
        // ReSharper disable once RemoveRedundantBraces
        {
            try
            {
                var clientSocket = await _serverSocket!.AcceptAsync(cancellationToken);

                var isAcquired = await _serverSemaphore.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
                if (!isAcquired)
                {
                    Console.WriteLine($"TCP server {clientSocket.RemoteEndPoint} connection failed");
                    CloseSocket(clientSocket);
                    continue;
                }

                var clientSemaphore = new SemaphoreSlim(1, 1);
                _semaphores.TryAdd(clientSocket, clientSemaphore);

                _ = Task.Run(async () => await ProcessClientAsync(clientSocket, clientSemaphore, cancellationToken), cancellationToken);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"TCP server socket error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TCP server error: {ex.Message}");
            }
        }
    }

    private async Task ProcessClientAsync(Socket socket, SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"TCP client {socket.RemoteEndPoint} connected");

        var index = 0;
        var arrayPool = ArrayPool<byte>.Shared;
        var buffer = arrayPool.Rent(MaxBufferSize);
        var memory = new Memory<byte>(buffer);

        try
        {
            while (socket.Connected && !cancellationToken.IsCancellationRequested)
            {
                (var result, index) = await ProcessClientInternalAsync(socket, semaphore, memory, index, cancellationToken);
                if (!result)
                    break;
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"TCP client socket error {socket.RemoteEndPoint}: {ex.Message}");
        }
        catch (Exception ex) when (ex is not TaskCanceledException && ex is not OperationCanceledException)
        {
            Console.WriteLine($"TCP client error {socket.RemoteEndPoint}: {ex.Message}");
        }
        finally
        {
            arrayPool.Return(buffer);

            CloseSemaphore(socket, semaphore);

            CloseSocket(socket);

            _serverSemaphore.Release();
        }
    }

    private async Task<(bool result, int index)> ProcessClientInternalAsync(Socket socket, SemaphoreSlim semaphore, Memory<byte> memory, int index, CancellationToken cancellationToken = default)
    {
        var isAcquired = false;

        try
        {
            isAcquired = await semaphore.WaitAsync(TimeSpan.FromMilliseconds(500), cancellationToken);
            if (!isAcquired)
            {
                Console.WriteLine($"TCP client {socket.RemoteEndPoint}. Semaphore capture timeout");
                return (true, index);
            }

            var bytesReaded = await socket.ReceiveAsync(memory[index..], SocketFlags.None, cancellationToken);
            if (bytesReaded < 1)
                return (false, index);

            Console.WriteLine($"TCP client {socket.RemoteEndPoint} received {bytesReaded} bytes: {CurrentEncoding.GetString(memory[index..(index + bytesReaded)].Span)}");

            index += bytesReaded;
            if (index > MaxCommandSize)
            {
                Console.WriteLine($"TCP client error {socket.RemoteEndPoint}. The command length has been exceeded");
                return (false, index);
            }

            var result = await RequestProcessing(socket, memory, index, cancellationToken);
            return (true, result ? 0 : index);
        }
        catch (TaskCanceledException)
        {
            return (false, index);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TCP client error {socket.RemoteEndPoint}: {ex.Message}");
            return (false, index);
        }
        finally
        {
            if (isAcquired)
                semaphore.Release();
        }
    }

    private static readonly Encoding CurrentEncoding = Encoding.UTF8;
    private static readonly byte[] OkResponse = CurrentEncoding.GetBytes("OK\r\n");
    private static readonly byte[] NullResponse = CurrentEncoding.GetBytes("(nil)\r\n");
    private static readonly byte[] ErrorResponse = CurrentEncoding.GetBytes("-ERR Unknown command\r\n");

    private async Task<bool> RequestProcessing(Socket socket, Memory<byte> memory, int bytesReaded, CancellationToken cancellationToken = default)
    {
        var startTime = Stopwatch.GetTimestamp();
        using var activity = OpenTelemetryConstants.MyActivitySource.StartActivity("ProcessingRequest");

        try
        {
            var result = await RequestProcessingInternal(activity, socket, memory, bytesReaded, cancellationToken);
            if (!result)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Error processing request");
                return false;
            }

            OperationsCounter.Add(1, new KeyValuePair<string, object?>("operation", "ProcessRequest"));
            activity?.SetStatus(ActivityStatusCode.Ok);
            return true;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"TCP client parsing json error {socket.RemoteEndPoint}: {ex.Message}");
            activity?.SetStatus(ActivityStatusCode.Error, "Error parsing request");
            return true;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
        finally
        {
            var elapsedTime = Stopwatch.GetElapsedTime(startTime);
            OperationsTimeHistogram.Record(elapsedTime.TotalMilliseconds, new KeyValuePair<string, object?>("operation", "ProcessRequest"));
        }
    }

    private async Task<bool> RequestProcessingInternal(Activity? activity, Socket socket, Memory<byte> memory, int bytesReaded, CancellationToken cancellationToken = default)
    {
        var span = memory[..bytesReaded].Span;
        Console.WriteLine($"TCP client {socket.RemoteEndPoint} processing {bytesReaded} bytes: {CurrentEncoding.GetString(span)}");

        var request = CommandParser<byte>.Parse(span, (byte)' ');
        if (request.Command.IsEmpty)
            return false;

        Console.WriteLine($"TCP client {socket.RemoteEndPoint} processed request: {request.ToString()}");

        const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
        var command = CommandParts<byte>.ToString(request.Command, CurrentEncoding);
        var key = CommandParts<byte>.ToString(request.Key, CurrentEncoding);

        activity?.SetTag("command.name", command);
        activity?.SetTag("command.size", bytesReaded);
        activity?.SetTag("command.key", key);

        switch (command)
        {
            case not null when command.Equals("get", comparison) && request.Key.IsEmpty:
                return false;
            case not null when command.Equals("get", comparison):
                var value = simpleStore.Get(key);
                var bytes = value is null ? NullResponse : JsonSerializer.SerializeToUtf8Bytes(value);
                await SendResponseAsync(socket, bytes, cancellationToken);
                return true;
            case not null when command.Equals("set", comparison) && (request.Key.IsEmpty || request.Value.IsEmpty):
                return false;
            case not null when command.Equals("set", comparison):
                var profile = JsonSerializer.Deserialize<UserProfile>(request.Value);
                if (profile is null)
                    return false;

                simpleStore.Set(key, profile);
                await SendResponseAsync(socket, OkResponse, cancellationToken);
                return true;
            case not null when command.Equals("delete", comparison) && request.Key.IsEmpty:
                return false;
            case not null when command.Equals("delete", comparison):
                simpleStore.Delete(key);
                await SendResponseAsync(socket, OkResponse, cancellationToken);
                return true;
            default:
                await SendResponseAsync(socket, ErrorResponse, cancellationToken);
                return true;
        }
    }

    private static async Task SendResponseAsync(Socket socket, ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default)
    {
        await socket.SendAsync(memory, cancellationToken);
        Console.WriteLine($"TCP client {socket.RemoteEndPoint} send {memory.Length} bytes: {CurrentEncoding.GetString(memory.Span)}");
    }

    private void CloseSemaphore(Socket? socket, SemaphoreSlim? semaphore)
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

    private static void CloseSocket(Socket? socket)
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

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            Console.WriteLine("TCP server stopping...");

            _serverSocket?.Close();
            _serverSocket?.Dispose();
            _serverSocket = null;

            _serverSemaphore.Dispose();

            foreach (var (socket, semaphore) in _semaphores)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket.Dispose();

                semaphore.Dispose();
            }
            _semaphores.Clear();

            Console.WriteLine("TCP server stopped");
        }

        _isDisposed = true;
    }

    ~TcpServer() =>
       Dispose(false);
}
