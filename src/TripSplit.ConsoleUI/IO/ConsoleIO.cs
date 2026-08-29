namespace TripSplit.ConsoleUI.IO;

public sealed class ConsoleIO : IConsoleIO
{
    public void Write(string text) => Console.Write(text);
    public void WriteLine(string text = "") => Console.WriteLine(text);

    public string ReadLine(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine() ?? string.Empty;
    }
}
