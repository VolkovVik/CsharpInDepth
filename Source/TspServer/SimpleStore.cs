namespace TspServer;

/// <summary>
/// Базовое хранилище
/// </summary>
public class SimpleStore : IDisposable
{
    public long _setCount;
    public long _getCount;
    public long _deleteCount;

    private bool _isDisposed;

    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<string, byte[]> _store = [];

    /// <summary>
    /// Добавление(обновление) значение по ключу.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Set(string key, byte[] value)
    {
        _lock.EnterWriteLock();
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

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
    /// <param name="key">/param>
    /// <returns></returns>
    public byte[] Get(string key)
    {
        _lock.EnterReadLock();
        try
        {
            if (string.IsNullOrWhiteSpace(key) || !_store.TryGetValue(key, out var value))
                return null;

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
        _lock.EnterWriteLock();
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

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

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            _lock?.Dispose();
            _store?.Clear();
        }

        _isDisposed = true;
    }
}
