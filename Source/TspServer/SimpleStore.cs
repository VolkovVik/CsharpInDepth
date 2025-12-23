using System.Text.Json;

namespace TspServer;

/// <summary>
/// Базовое хранилище
/// </summary>
public sealed class SimpleStore : IDisposable
{
    private long _setCount;
    private long _getCount;
    private long _deleteCount;

    private bool _isDisposed;

    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<string, byte[]> _store = [];

    /// <summary>
    /// Добавление(обновление) значение по ключу.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="profile"></param>
    public void Set(string key, UserProfile profile)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        _lock.EnterWriteLock();
        try
        {
            using var ms = new MemoryStream();
            profile.SerializeToBinary(ms);
            var value = ms.ToArray();

            _store[key] = value;
            Interlocked.Increment(ref _setCount);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Получение значения по ключу
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public UserProfile? Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        _lock.EnterReadLock();
        try
        {
            if (!_store.TryGetValue(key, out var bytes) || bytes.Length < 1)
                return null;

            var value = JsonSerializer.Deserialize<UserProfile>(bytes);
            Interlocked.Increment(ref _getCount);
            return value;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Удаление значения по ключу
    /// </summary>
    /// <param name="key"></param>
    public void Delete(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        _lock.EnterWriteLock();
        try
        {
            var result = _store.Remove(key);
            if (!result)
                return;

            Interlocked.Increment(ref _deleteCount);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public (long setCount, long getCount, long deleteCount) GetStatistics() =>
        (Interlocked.Read(ref _setCount), Interlocked.Read(ref _getCount), Interlocked.Read(ref _deleteCount));

    ~SimpleStore() =>
       Dispose(false);

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
            _lock.Dispose();
            _store.Clear();
        }

        _isDisposed = true;
    }
}
