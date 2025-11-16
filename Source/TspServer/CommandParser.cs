using System.Runtime.InteropServices;
using System.Text;

namespace TspServer;

/// <summary>
/// Парсер команд
/// </summary>
public static class CommandParser<T>
    where T : unmanaged, IEquatable<T>
{
    public static CommandParts<T> Parse(ReadOnlySpan<T> input, T delimeter)
    {
        var command = ParseInternal(input, delimeter, 0);
        var key = ParseInternal(input, delimeter, command.Length);
        if (key.Value.IsEmpty)
            return default;

        var value = ParseInternal(input, delimeter, key.Length);
        return new CommandParts<T>
        {
            Command = command.Value,
            Key = key.Value,
            Value = value.Value
        };
    }

    private static ParsedParts ParseInternal(ReadOnlySpan<T> input, T delimeter, int startindex)
    {
        if (startindex >= input.Length)
            return new ParsedParts([], input.Length);

        var index = input[startindex..].IndexOf(delimeter);
        switch (index)
        {
            case -1 when startindex == input.Length:
                return new ParsedParts([], input.Length);
            case 0:
                return ParseInternal(input, delimeter, startindex + 1);
            case -1 when startindex < input.Length:
                index = input.Length - startindex;
                break;
        }

        var value = input.Slice(startindex, index);
        return new ParsedParts(value, startindex + index + 1);
    }

    private readonly ref struct ParsedParts
    {
        public readonly int Length = 0;
        public readonly ReadOnlySpan<T> Value = [];

        public ParsedParts(ReadOnlySpan<T> value, int length)
        {
            Value = value;
            Length = length;
        }
    }
}

public ref struct CommandParts<T>
    where T : unmanaged, IEquatable<T>
{
    public ReadOnlySpan<T> Command;
    public ReadOnlySpan<T> Key;
    public ReadOnlySpan<T> Value;

    public static string ToString(ReadOnlySpan<T> span, Encoding? encoding = null)
    {
        if (span.IsEmpty)
            return string.Empty;

        if (typeof(T) == typeof(char))
            return new string(MemoryMarshal.Cast<T, char>(span));

        if (typeof(T) == typeof(byte))
        {
            encoding ??= Encoding.UTF8;
            var byteSpan = MemoryMarshal.Cast<T, byte>(span);
            return encoding.GetString(byteSpan);
        }

        return string.Empty;
    }

    public override readonly string ToString()
    {
        if (Command.IsEmpty)
            return string.Empty;

        var sb = new StringBuilder();
        sb.Append(ToString(Command));

        if (!Key.IsEmpty)
            sb.Append(' ').Append(ToString(Key));

        if (!Value.IsEmpty)
            sb.Append(' ').Append(ToString(Value));

        return sb.ToString();
    }
}
