using System.Text;

namespace CrusaderWars.Core.Logging;

/// <summary>
/// File-backed application logger. Replicates the legacy Program.Logger output
/// byte-for-byte: "<i>[timestamp] DEBUG: message</i>" lines appended to debug.log (and
/// echoed to the console), exceptions written to error.log with a stack trace block.
/// </summary>
public sealed class FileAppLogger : IAppLogger
{
    private readonly string _debugLogPath;
    private readonly string _errorLogPath;

    public FileAppLogger(string logDirectory, bool truncateDebugLogOnCreate = false)
    {
        Directory.CreateDirectory(logDirectory);
        _debugLogPath = Path.Combine(logDirectory, "debug.log");
        _errorLogPath = Path.Combine(logDirectory, "error.log");

        if (truncateDebugLogOnCreate)
            File.WriteAllText(_debugLogPath, string.Empty);
    }

    public string DebugLogPath => _debugLogPath;
    public string ErrorLogPath => _errorLogPath;

    public void Debug(string message) => Write(_debugLogPath, $"[{DateTime.Now}] DEBUG: {message}");

    public void Info(string message) => Write(_debugLogPath, $"[{DateTime.Now}] INFO: {message}");

    public void Warn(string message) => Write(_debugLogPath, $"[{DateTime.Now}] WARN: {message}");

    public void Error(string message) => Write(_errorLogPath, $"[{DateTime.Now}] {message}");

    public void Error(Exception exception)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"[{DateTime.Now}] {exception.GetType()}: {exception.Message}");
        builder.AppendLine($"StackTrace: {exception.StackTrace}");
        builder.AppendLine();
        Append(_errorLogPath, builder.ToString());
    }

    private static void Write(string path, string line)
    {
        Console.WriteLine(line);
        Append(path, line + Environment.NewLine);
    }

    private static void Append(string path, string content)
    {
        try
        {
            File.AppendAllText(path, content);
        }
        catch (Exception ex)
        {
            // Fallback for critical logging failures, matching the legacy logger.
            Console.WriteLine($"FATAL: Could not write to log. {ex.Message}");
        }
    }
}
