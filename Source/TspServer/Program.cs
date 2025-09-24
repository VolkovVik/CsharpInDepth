// See https://aka.ms/new-console-template for more information
using TspServer;

var str = "     SET  user:1   data";
var data = System.Text.Encoding.UTF8.GetBytes(str);
var result = CommandParser.Parse(data);

var command = System.Text.Encoding.UTF8.GetString(result.Command);
var key = System.Text.Encoding.UTF8.GetString(result.Key);
var value = System.Text.Encoding.UTF8.GetString(result.Value);

Console.WriteLine($"Command: {command}");
Console.WriteLine($"Key: {key}");
Console.WriteLine($"Value: {value}");
Console.ReadKey();
