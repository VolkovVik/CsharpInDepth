using System.Text;
using Shouldly;

namespace TspServer.UnitTests;

public class CommandParserShould
{
    [Theory]
    [InlineData("SET user:1 data", "SET", "user:1", "data")]
    [InlineData("SET  user:1  data", "SET", "user:1", "data")]
    [InlineData("    SET    user:1     data   ", "SET", "user:1", "data")]
    [InlineData("GET user:1", "GET", "user:1", "")]
    [InlineData("GET  user:1", "GET", "user:1", "")]
    [InlineData("    GET    user:1  ", "GET", "user:1", "")]
    public void ReturnCorrectValues(string str, string command, string key, string value)
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes(str);

        // Act
        var result = CommandParser.Parse(data);

        // Assert
        var resultCommand = Encoding.UTF8.GetString(result.Command);
        var resultKey = Encoding.UTF8.GetString(result.Key);
        var resultValue = Encoding.UTF8.GetString(result.Value);

        resultCommand.ShouldBe(command);
        resultKey.ShouldBe(key);
        resultValue.ShouldBe(value);
    }

    [Theory]
    [InlineData("SET")]
    [InlineData("  SET  ")]
    [InlineData("SETuser:1")]
    [InlineData("GET")]
    [InlineData("  GET  ")]
    [InlineData("GETuser:1")]
    public void ReturnIncorrectValues(string str)
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes(str);

        // Act
        var result = CommandParser.Parse(data);

        // Assert
        var resultCommand = Encoding.UTF8.GetString(result.Command);
        var resultKey = Encoding.UTF8.GetString(result.Key);
        var resultValue = Encoding.UTF8.GetString(result.Value);

        resultCommand.ShouldBe(string.Empty);
        resultKey.ShouldBe(string.Empty);
        resultValue.ShouldBe(string.Empty);
    }
}
