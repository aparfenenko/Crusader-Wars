namespace CrusaderWars.Core.Logging;

public sealed class ConsoleLogger : IAppLogger
{
    public void Debug(string message) => Write("DBG", message);
    public void Info(string message) => Write("INF", message);
    public void Warn(string message) => Write("WRN", message);
    public void Error(string message) => Write("ERR", message);

    private static void Write(string level, string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {level} {message}");
    }
}
