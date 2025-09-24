namespace TspServer;

/// <summary>
/// Базовое хранилище
/// </summary>
public class SimpleStore
{
    private readonly Dictionary<string, byte[]> _store = [];

    /// <summary>
    /// Добавление(обновление) значение по ключу.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Set(string key, byte[] value) =>
        _store[key] = value;

    /// <summary>
    /// Получение значения по ключу
    /// </summary>
    /// <param name="key">/param>
    /// <returns></returns>
    public byte[] Get(string key) =>
        _store.TryGetValue(key, out var value) ? value : null;

    /// <summary>
    /// Удаление значения по ключу
    /// </summary>
    /// <param name="key"></param>
    public void Delete(string key) =>
        _store.Remove(key);
}
