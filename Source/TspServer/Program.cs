// See https://aka.ms/new-console-template for more information
using TspServer;

var str = "     SET  user:1   data";
var data = System.Text.Encoding.UTF8.GetBytes(str);
var result = CommandParser<byte>.Parse(data, (byte)' ');

Console.WriteLine(result.ToString());
Console.ReadKey();
