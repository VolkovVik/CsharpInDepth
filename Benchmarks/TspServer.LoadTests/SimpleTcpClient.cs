using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TspServer.LoadTests;

public sealed class SimpleTcpClient : IDisposable
{
    private bool _isDisposed;

    private TcpClient _client;
    private NetworkStream _stream;

    public async Task ConnectAsync(string ip = "127.0.0.1", int port = 8080)
    {
        _client = new TcpClient { NoDelay = true };
        await _client.ConnectAsync(IPAddress.Parse(ip), port);
        _stream = _client.GetStream();
    }

    public async Task<string> SetAsync(string key, string value) =>
        await RequestAsync($"SET {key} {value}");

    // ReSharper disable once UnusedMember.Global
    public async Task<string> GetAsync(string key) =>
        await RequestAsync($"GET {key}");

    private async Task<string> RequestAsync(string message, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1024);
        var memory = new Memory<byte>(buffer);

        try
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                var length = Encoding.UTF8.GetBytes(message, memory.Span);
                await _stream.WriteAsync(memory[..length], cancellationToken);
            }

            var readed = await _stream.ReadAsync(memory, cancellationToken);
            return readed == 0 ? string.Empty : Encoding.UTF8.GetString(memory[..readed].Span);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return string.Empty;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
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
            _stream.Close();
            _client.Close();

            _stream.Dispose();
            _client.Dispose();
        }

        _isDisposed = true;
    }

    ~SimpleTcpClient() =>
       Dispose(false);
}
