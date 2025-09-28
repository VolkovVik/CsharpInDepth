// See https://aka.ms/new-console-template for more information
using TspServer;

#pragma warning disable CS0219 // Variable is assigned but its value is never used
#pragma warning disable S1481 // Unused local variables should be removed
var xxx = (byte)' ';
#pragma warning restore S1481 // Unused local variables should be removed
#pragma warning restore CS0219 // Variable is assigned but its value is never used

var str = "     SET  user:1   data";
var data = System.Text.Encoding.UTF8.GetBytes(str);
var result = CommandParser<byte>.Parse(data, (byte)' ');

Console.WriteLine(result.ToString());
Console.ReadKey();
