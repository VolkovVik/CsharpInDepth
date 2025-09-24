namespace TspServer;

/// <summary>
/// Парсер команд
/// </summary>
public static class CommandParser
{
    private const byte Space = 0x20;

    public static CommandParts Parse(ReadOnlySpan<byte> input)
    {
        var command = ParseInternal(input, 0);
        var key = ParseInternal(input, command.Length);
        if (key.Value.IsEmpty)
            return default;

        var value = ParseInternal(input, key.Length);
        return new CommandParts
        {
            Command = command.Value,
            Key = key.Value,
            Value = value.Value
        };
    }

    private static ParsedParts ParseInternal(ReadOnlySpan<byte> input, int startindex)
    {
        if (startindex >= input.Length)
            return new ParsedParts([], input.Length);

        var index = input[startindex..].IndexOf(Space);
        if (index == -1 && startindex == input.Length)
            return new ParsedParts([], input.Length);

        if (index is 0)
            return ParseInternal(input, startindex + 1);

        if (index == -1 && startindex < input.Length)
            index = input.Length - startindex;

        var value = input.Slice(startindex, index);
        return new ParsedParts(value, startindex + index + 1);
    }

    private ref struct ParsedParts
    {
        public int Length = 0;
        public ReadOnlySpan<byte> Value = [];

        public ParsedParts(ReadOnlySpan<byte> value, int length)
        {
            Value = value;
            Length = length;
        }
    }
}

public ref struct CommandParts
{
    public ReadOnlySpan<byte> Command;
    public ReadOnlySpan<byte> Key;
    public ReadOnlySpan<byte> Value;
}


