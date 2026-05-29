namespace Switcher.App.Services;

internal static class AppLogger
{
    private static readonly object SyncRoot = new();
    private static readonly string LogDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Switcher", "logs");
    private static readonly string LogFilePath = Path.Combine(LogDirectory, "app.log");

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Error(string message, Exception? exception = null)
    {
        var details = exception is null ? message : $"{message} | {exception.GetType().Name}: {exception.Message}";
        Write("ERROR", details);
    }

    public static void Step(string operationId, string step, string details)
    {
        Write("TRACE", $"op={operationId} step={step} {details}");
    }

    public static string Preview(string value, int maxLen = 24)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "<empty>";
        }

        var normalized = value.Replace("\r", "\\r").Replace("\n", "\\n");
        if (normalized.Length <= maxLen)
        {
            return normalized;
        }

        return normalized[..maxLen] + "...";
    }

    private static void Write(string level, string message)
    {
        try
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(LogDirectory);
                var line = $"{DateTime.UtcNow:O} [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, line);
            }
        }
        catch
        {
            // Never crash app because logging failed.
        }
    }
}
