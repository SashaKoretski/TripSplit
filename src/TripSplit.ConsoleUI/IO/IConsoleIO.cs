namespace TripSplit.ConsoleUI.IO;

public interface IConsoleIO
{
    void Write(string text);
    void WriteLine(string text = "");
    string ReadLine(string prompt);
}
