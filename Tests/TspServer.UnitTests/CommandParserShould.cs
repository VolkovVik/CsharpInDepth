using System.Text;
using Shouldly;

namespace TspServer.UnitTests;

public class CommandParserShould
{
    [Theory]
    [InlineData("SET user:1 data", "SET user:1 data")]
    [InlineData("SET  user:1  data", "SET user:1 data")]
    [InlineData("    SET    user:1     data   ", "SET user:1 data")]
    [InlineData("GET user:1", "GET user:1")]
    [InlineData("GET  user:1", "GET user:1")]
    [InlineData("    GET    user:1  ", "GET user:1")]
    public void ReturnCorrectValuesFromByteArray(string str, string value)
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes(str);

        // Act
        var result = CommandParser<byte>.Parse(data, (byte)' ');

        // Assert
        result.ToString().ShouldBe(value);
    }

    [Theory]
    [InlineData("SET user:1 data", "SET user:1 data")]
    [InlineData("SET  user:1  data", "SET user:1 data")]
    [InlineData("    SET    user:1     data   ", "SET user:1 data")]
    [InlineData("GET user:1", "GET user:1")]
    [InlineData("GET  user:1", "GET user:1")]
    [InlineData("    GET    user:1  ", "GET user:1")]
    public void ReturnCorrectValuesFromCharArray(string str, string value)
    {
        // Arrange
        var data = str.ToCharArray();

        // Act
        var result = CommandParser<char>.Parse(data, ' ');

        // Assert
        result.ToString().ShouldBe(value);
    }

    [Theory]
    [InlineData("SET")]
    [InlineData("  SET  ")]
    [InlineData("SETuser:1")]
    [InlineData("GET")]
    [InlineData("  GET  ")]
    [InlineData("GETuser:1")]
    public void ReturnIncorrectValueFromByteArray(string str)
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes(str);

        // Act
        var result = CommandParser<byte>.Parse(data, (byte)' ');

        // Assert
        result.ToString().ShouldBeNullOrEmpty();
    }

    [Theory]
    [InlineData("SET")]
    [InlineData("  SET  ")]
    [InlineData("SETuser:1")]
    [InlineData("GET")]
    [InlineData("  GET  ")]
    [InlineData("GETuser:1")]
    public void ReturnIncorrectValueFromCharArray(string str)
    {
        // Arrange
        var data = str.ToCharArray();

        // Act
        var result = CommandParser<char>.Parse(data, ' ');

        // Assert
        result.ToString().ShouldBeNullOrEmpty();
    }
}
