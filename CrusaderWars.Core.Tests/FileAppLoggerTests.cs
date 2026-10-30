using CrusaderWars.Core.Logging;
using Xunit;

namespace CrusaderWars.Core.Tests;

public class FileAppLoggerTests : IDisposable
{
    private readonly string _dir;

    public FileAppLoggerTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "cc-logger-tests", Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Debug_lines_use_legacy_format_in_debug_log_and_console()
    {
        var logger = new FileAppLogger(_dir);

        logger.Debug("hello world");

        string content = File.ReadAllText(logger.DebugLogPath);
        // Format mirrors the legacy logger: "[<culture-specific timestamp>] DEBUG: <message>".
        Assert.StartsWith("[", content);
        Assert.Contains("] DEBUG: hello world", content);
        Assert.DoesNotContain("INFO", content);
    }

    [Fact]
    public void Info_and_warn_lines_are_appended()
    {
        var logger = new FileAppLogger(_dir);

        logger.Info("info message");
        logger.Warn("warn message");

        string content = File.ReadAllText(logger.DebugLogPath);
        Assert.Contains("] INFO: info message", content);
        Assert.Contains("] WARN: warn message", content);
    }

    [Fact]
    public void Exception_writes_legacy_error_log_block()
    {
        var logger = new FileAppLogger(_dir);
        Exception exception;

        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        logger.Error(exception);

        string content = File.ReadAllText(logger.ErrorLogPath);
        Assert.Contains("] System.InvalidOperationException: boom", content);
        Assert.Contains("StackTrace: ", content);
        Assert.EndsWith(Environment.NewLine, content);
        Assert.DoesNotContain("DEBUG:", content);
    }

    [Fact]
    public void Error_message_goes_to_error_log_only()
    {
        var logger = new FileAppLogger(_dir);

        logger.Error("plain error");

        Assert.Contains("plain error", File.ReadAllText(logger.ErrorLogPath));
        Assert.False(File.Exists(logger.DebugLogPath) && File.ReadAllText(logger.DebugLogPath).Contains("plain error"));
    }

    [Fact]
    public void Truncate_option_resets_debug_log()
    {
        new FileAppLogger(_dir).Debug("old entry");

        var fresh = new FileAppLogger(_dir, truncateDebugLogOnCreate: true);
        fresh.Debug("new entry");

        string content = File.ReadAllText(fresh.DebugLogPath);
        Assert.DoesNotContain("old entry", content);
        Assert.Contains("new entry", content);
    }

    [Fact]
    public void Directory_is_created_when_missing()
    {
        string nested = Path.Combine(_dir, "a", "b");

        new FileAppLogger(nested).Debug("x");

        Assert.True(File.Exists(Path.Combine(nested, "debug.log")));
    }
}
